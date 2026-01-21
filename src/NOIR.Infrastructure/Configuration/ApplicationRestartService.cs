namespace NOIR.Infrastructure.Configuration;

/// <summary>
/// Service for managing application restart with environment detection.
/// Handles graceful shutdown and provides warnings based on hosting environment.
/// </summary>
public class ApplicationRestartService : IApplicationRestartService, IScopedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IOptionsMonitor<ConfigurationManagementSettings> _settings;
    private readonly ILogger<ApplicationRestartService> _logger;
    private static DateTimeOffset? _lastRestartTime;
    private static readonly object _lock = new();

    public ApplicationRestartService(
        IHostApplicationLifetime lifetime,
        IOptionsMonitor<ConfigurationManagementSettings> settings,
        ILogger<ApplicationRestartService> logger)
    {
        _lifetime = lifetime;
        _settings = settings;
        _logger = logger;
    }

    public HostingEnvironment DetectEnvironment()
    {
        var processName = Process.GetCurrentProcess().ProcessName;

        // IIS detection (w3wp.exe)
        if (processName.Equals("w3wp", StringComparison.OrdinalIgnoreCase))
        {
            return HostingEnvironment.IIS;
        }

        // Docker detection (DOTNET_RUNNING_IN_CONTAINER env var)
        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            // Kubernetes detection (KUBERNETES_SERVICE_HOST env var)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")))
            {
                return HostingEnvironment.Kubernetes;
            }

            return HostingEnvironment.Docker;
        }

        // Kestrel (standalone .NET process)
        if (processName.StartsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
            processName.StartsWith("NOIR", StringComparison.OrdinalIgnoreCase))
        {
            return HostingEnvironment.Kestrel;
        }

        return HostingEnvironment.Unknown;
    }

    public bool CanRestart()
    {
        var env = DetectEnvironment();

        // IIS, Docker, and Kubernetes support auto-restart
        // Kestrel requires external process manager (systemd, PM2, supervisor)
        return env is HostingEnvironment.IIS
            or HostingEnvironment.Docker
            or HostingEnvironment.Kubernetes;
    }

    public async Task<Result> InitiateRestartAsync(
        string reason,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Check if restart is allowed (rate limiting)
        if (!IsRestartAllowed())
        {
            var remainingMinutes = GetRemainingCooldownMinutes();
            return Result.Failure(
                Error.Failure("NOIR-CFG-004",
                    $"Restart cooldown active. Please wait {remainingMinutes} more minutes."));
        }

        var env = DetectEnvironment();

        // Log restart with environment-specific warnings
        _logger.LogWarning(
            "⚠️ Application restart initiated by {UserId}: {Reason}",
            userId, reason);

        _logger.LogWarning("Environment: {Environment}", env);

        switch (env)
        {
            case HostingEnvironment.Kestrel:
                _logger.LogWarning(
                    "⚠️ Kestrel detected - ensure process manager (systemd, PM2, supervisor) is configured for auto-restart!");
                break;

            case HostingEnvironment.Docker:
                _logger.LogWarning(
                    "⚠️ Docker detected - ensure restart policy is set (restart: unless-stopped)!");
                break;

            case HostingEnvironment.Kubernetes:
                _logger.LogInformation(
                    "✅ Kubernetes detected - pod will auto-restart via restartPolicy: Always");
                break;

            case HostingEnvironment.IIS:
                _logger.LogInformation(
                    "✅ IIS detected - app pool will auto-restart");
                break;

            case HostingEnvironment.Unknown:
                _logger.LogError(
                    "⚠️ Unknown hosting environment - restart may not work!");
                break;
        }

        // Update last restart time
        lock (_lock)
        {
            _lastRestartTime = DateTimeOffset.UtcNow;
        }

        // Delay shutdown to allow HTTP response to complete
        // Without delay, the response would fail (connection closed mid-response)
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            _logger.LogWarning("Initiating graceful shutdown...");

            // Signal graceful shutdown
            // This sets a CancellationToken that propagates through the entire app
            // Kestrel stops accepting new requests and waits for in-flight requests
            // After ShutdownTimeoutSeconds, remaining requests are forcibly terminated
            _lifetime.StopApplication();
        }, cancellationToken);

        return Result.Success();
    }

    public bool IsRestartAllowed()
    {
        lock (_lock)
        {
            if (_lastRestartTime == null)
                return true;

            var elapsed = DateTimeOffset.UtcNow - _lastRestartTime.Value;
            var minimumInterval = TimeSpan.FromMinutes(_settings.CurrentValue.MinimumRestartIntervalMinutes);

            return elapsed >= minimumInterval;
        }
    }

    public DateTimeOffset? GetLastRestartTime()
    {
        lock (_lock)
        {
            return _lastRestartTime;
        }
    }

    // Helper: Get remaining cooldown time in minutes
    private int GetRemainingCooldownMinutes()
    {
        lock (_lock)
        {
            if (_lastRestartTime == null)
                return 0;

            var elapsed = DateTimeOffset.UtcNow - _lastRestartTime.Value;
            var minimumInterval = TimeSpan.FromMinutes(_settings.CurrentValue.MinimumRestartIntervalMinutes);
            var remaining = minimumInterval - elapsed;

            return remaining.TotalMinutes > 0 ? (int)Math.Ceiling(remaining.TotalMinutes) : 0;
        }
    }
}
