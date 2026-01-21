namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing application restart with environment detection.
/// Detects hosting environment (IIS, Kestrel, Docker, Kubernetes) and handles graceful shutdown.
/// </summary>
public interface IApplicationRestartService
{
    /// <summary>
    /// Detects the current hosting environment.
    /// </summary>
    HostingEnvironment DetectEnvironment();

    /// <summary>
    /// Determines if the application can be restarted in the current environment.
    /// Returns false if restart policy is not configured or environment doesn't support auto-restart.
    /// </summary>
    bool CanRestart();

    /// <summary>
    /// Initiates a graceful application shutdown.
    /// The host infrastructure is responsible for restarting the process.
    /// </summary>
    Task<Result> InitiateRestartAsync(
        string reason,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if restart is allowed based on rate limiting.
    /// Prevents rapid restart loops.
    /// </summary>
    bool IsRestartAllowed();

    /// <summary>
    /// Gets the time of the last restart attempt.
    /// </summary>
    DateTimeOffset? GetLastRestartTime();
}

/// <summary>
/// Hosting environment detection.
/// </summary>
public enum HostingEnvironment
{
    /// <summary>
    /// Running under IIS (w3wp.exe) - auto-restart supported
    /// </summary>
    IIS,

    /// <summary>
    /// Running in Docker container - requires restart policy
    /// </summary>
    Docker,

    /// <summary>
    /// Running in Kubernetes - supported via restartPolicy: Always
    /// </summary>
    Kubernetes,

    /// <summary>
    /// Running as standalone Kestrel - requires external process manager (systemd, PM2, supervisor)
    /// </summary>
    Kestrel,

    /// <summary>
    /// Unknown hosting environment - restart may not work
    /// </summary>
    Unknown
}
