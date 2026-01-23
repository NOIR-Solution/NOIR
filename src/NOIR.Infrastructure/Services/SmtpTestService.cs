namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for testing SMTP connections.
/// Uses fallback chain: Tenant SMTP settings → Platform SMTP settings.
/// </summary>
public class SmtpTestService : ISmtpTestService, IScopedService
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;
    private readonly ILogger<SmtpTestService> _logger;

    public SmtpTestService(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        ILogger<SmtpTestService> logger)
    {
        _settingsService = settingsService;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
    }

    public async Task<Result<bool>> SendTestEmailAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        // Get SMTP settings with fallback: Tenant → Platform
        var (settings, source) = await GetSmtpSettingsWithFallbackAsync(cancellationToken);

        if (settings == null || settings.Count == 0)
        {
            return Result.Failure<bool>(Error.Validation("smtp", "SMTP settings have not been configured yet"));
        }

        var host = settings.GetValueOrDefault("smtp:host", string.Empty);
        var port = int.TryParse(settings.GetValueOrDefault("smtp:port"), out var p) ? p : 25;
        var username = settings.GetValueOrDefault("smtp:username");
        var password = settings.GetValueOrDefault("smtp:password");
        var useSsl = bool.TryParse(settings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl;
        var fromEmail = settings.GetValueOrDefault("smtp:from_email", "noreply@noir.local");
        var fromName = settings.GetValueOrDefault("smtp:from_name", "NOIR");

        if (string.IsNullOrEmpty(host))
        {
            return Result.Failure<bool>(Error.Validation("smtp", "SMTP host is not configured"));
        }

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(host, port, useSsl
                ? MailKit.Security.SecureSocketOptions.SslOnConnect
                : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            var sourceLabel = source == "tenant" ? "tenant-specific" : "platform";
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(fromName, fromEmail));
            message.To.Add(MimeKit.MailboxAddress.Parse(recipientEmail));
            message.Subject = "NOIR - SMTP Test Email";
            message.Body = new MimeKit.TextPart("html")
            {
                Text = $"<h2>SMTP Connection Test</h2><p>This is a test email using <strong>{sourceLabel}</strong> SMTP settings. If you received this, your SMTP configuration is working correctly.</p><p><em>Sent at: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</em></p>"
            };

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("SMTP test email sent successfully to {Recipient} using {Source} settings", recipientEmail, source);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP test connection failed for host {Host}:{Port} using {Source} settings", host, port, source);
            return Result.Failure<bool>(Error.Validation("smtp", $"SMTP connection failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets SMTP settings with fallback: Tenant → Platform.
    /// Returns the settings dictionary and the source ("tenant" or "platform").
    /// </summary>
    private async Task<(IReadOnlyDictionary<string, string>? Settings, string Source)> GetSmtpSettingsWithFallbackAsync(
        CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

        // Step 1: Check for tenant-specific SMTP settings
        if (currentTenantId != null)
        {
            var tenantSettings = await _settingsService.GetSettingsAsync(
                tenantId: currentTenantId,
                keyPrefix: "smtp:",
                cancellationToken: cancellationToken);

            if (tenantSettings.Count > 0 && tenantSettings.ContainsKey("smtp:host"))
            {
                var tenantHost = tenantSettings.GetValueOrDefault("smtp:host", string.Empty);
                if (!string.IsNullOrWhiteSpace(tenantHost))
                {
                    _logger.LogDebug("Using tenant-specific SMTP settings for test (tenant: {TenantId})", currentTenantId);
                    return (tenantSettings, "tenant");
                }
            }
        }

        // Step 2: Fall back to platform-level SMTP settings
        var platformSettings = await _settingsService.GetSettingsAsync(
            tenantId: null,
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        _logger.LogDebug("Using platform-level SMTP settings for test");
        return (platformSettings, "platform");
    }
}
