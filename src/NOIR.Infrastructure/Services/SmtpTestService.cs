namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for testing SMTP connections using platform-configured settings.
/// </summary>
public class SmtpTestService : ISmtpTestService, IScopedService
{
    private readonly ITenantSettingsService _settingsService;
    private readonly ILogger<SmtpTestService> _logger;

    public SmtpTestService(
        ITenantSettingsService settingsService,
        ILogger<SmtpTestService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<Result<bool>> SendTestEmailAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetSettingsAsync(
            tenantId: null,
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        if (settings.Count == 0)
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

            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(fromName, fromEmail));
            message.To.Add(MimeKit.MailboxAddress.Parse(recipientEmail));
            message.Subject = "NOIR - SMTP Test Email";
            message.Body = new MimeKit.TextPart("html")
            {
                Text = "<h2>SMTP Connection Test</h2><p>This is a test email from NOIR platform settings. If you received this, your SMTP configuration is working correctly.</p><p><em>Sent at: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + "</em></p>"
            };

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("SMTP test email sent successfully to {Recipient}", recipientEmail);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP test connection failed for host {Host}:{Port}", host, port);
            return Result.Failure<bool>(Error.Validation("smtp", $"SMTP connection failed: {ex.Message}"));
        }
    }
}
