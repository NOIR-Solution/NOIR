namespace NOIR.Infrastructure.Services;

/// <summary>
/// Email service implementation.
/// Uses database SMTP settings (platform level) with fallback to appsettings.json via FluentEmail.
/// Uses database templates for email content.
/// </summary>
public class EmailService : IEmailService, IScopedService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<EmailSettings> _emailSettings;
    private readonly ITenantSettingsService _settingsService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IFluentEmail fluentEmail,
        ApplicationDbContext dbContext,
        IOptionsMonitor<EmailSettings> emailSettings,
        ITenantSettingsService settingsService,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail;
        _dbContext = dbContext;
        _emailSettings = emailSettings;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try DB-configured SMTP first
            var dbResult = await TrySendViaDbSettingsAsync([to], subject, body, isHtml, cancellationToken);
            if (dbResult.HasValue)
                return dbResult.Value;

            // Fallback to FluentEmail (appsettings.json config)
            var email = _fluentEmail
                .To(to)
                .Subject(subject);

            if (isHtml)
                email.Body(body, isHtml: true);
            else
                email.Body(body);

            var response = await email.SendAsync(cancellationToken);

            if (!response.Successful)
            {
                _logger.LogError("Failed to send email to {To}. Errors: {Errors}", to, string.Join(", ", response.ErrorMessages));
            }

            return response.Successful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipients = to.ToList();

            // Try DB-configured SMTP first
            var dbResult = await TrySendViaDbSettingsAsync(recipients, subject, body, isHtml, cancellationToken);
            if (dbResult.HasValue)
                return dbResult.Value;

            // Fallback to FluentEmail (appsettings.json config)
            var email = _fluentEmail
                .To(recipients.Select(x => new FluentEmail.Core.Models.Address(x)))
                .Subject(subject);

            if (isHtml)
                email.Body(body, isHtml: true);
            else
                email.Body(body);

            var response = await email.SendAsync(cancellationToken);

            if (!response.Successful)
            {
                _logger.LogError("Failed to send email. Errors: {Errors}", string.Join(", ", response.ErrorMessages));
            }

            return response.Successful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email");
            return false;
        }
    }

    public async Task<bool> SendTemplateAsync<T>(string to, string subject, string templateName, T model, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load template with fallback: tenant-specific first, then platform-level (TenantId = null)
            var template = await GetTemplateWithFallbackAsync(templateName, cancellationToken);

            if (template == null)
            {
                _logger.LogError("Email template '{TemplateName}' not found (checked tenant and platform level)", templateName);
                return false;
            }

            if (!template.IsActive)
            {
                _logger.LogWarning("Email template '{TemplateName}' is not active", templateName);
                return false;
            }

            // Replace placeholders with model values
            var htmlBody = ReplacePlaceholders(template.HtmlBody, model);
            var emailSubject = string.IsNullOrWhiteSpace(subject)
                ? ReplacePlaceholders(template.Subject, model)
                : subject;

            // Try DB-configured SMTP first
            var dbResult = await TrySendViaDbSettingsAsync([to], emailSubject, htmlBody, true, cancellationToken);
            if (dbResult.HasValue)
                return dbResult.Value;

            // Fallback to FluentEmail (appsettings.json config)
            var response = await _fluentEmail
                .To(to)
                .Subject(emailSubject)
                .Body(htmlBody, isHtml: true)
                .SendAsync(cancellationToken);

            if (!response.Successful)
            {
                _logger.LogError("Failed to send template email to {To}. Errors: {Errors}", to, string.Join(", ", response.ErrorMessages));
            }

            return response.Successful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending template email to {To}", to);
            return false;
        }
    }

    /// <summary>
    /// Tries to send email using database-configured SMTP settings.
    /// Returns null if no DB settings are configured (caller should fallback to FluentEmail).
    /// Returns true/false if DB settings exist and send was attempted.
    /// </summary>
    private async Task<bool?> TrySendViaDbSettingsAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(
            tenantId: null, // Platform level only for SMTP
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        // If no DB settings, return null to signal fallback should be used
        if (settings.Count == 0 || !settings.ContainsKey("smtp:host"))
            return null;

        var host = settings.GetValueOrDefault("smtp:host", string.Empty);
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var port = int.TryParse(settings.GetValueOrDefault("smtp:port"), out var p) ? p : 25;
        var username = settings.GetValueOrDefault("smtp:username");
        var password = settings.GetValueOrDefault("smtp:password");
        var useSsl = bool.TryParse(settings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl;
        var fromEmail = settings.GetValueOrDefault("smtp:from_email", _emailSettings.CurrentValue.DefaultFromEmail);
        var fromName = settings.GetValueOrDefault("smtp:from_name", _emailSettings.CurrentValue.DefaultFromName);

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
            foreach (var recipient in recipients)
            {
                message.To.Add(MimeKit.MailboxAddress.Parse(recipient));
            }
            message.Subject = subject;
            message.Body = new MimeKit.TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogDebug("Email sent via DB-configured SMTP to {Recipients}", string.Join(", ", recipients));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via DB-configured SMTP ({Host}:{Port})", host, port);
            return false;
        }
    }

    /// <summary>
    /// Gets an email template with fallback logic:
    /// 1. First tries to find tenant-specific template (TenantId = current tenant)
    /// 2. Falls back to platform-level template (TenantId = null)
    /// This allows tenants to override platform defaults while sharing common templates.
    /// </summary>
    private async Task<EmailTemplate?> GetTemplateWithFallbackAsync(string templateName, CancellationToken cancellationToken)
    {
        var currentTenantId = _dbContext.TenantInfo?.Id;

        // Query all templates with this name, ignoring tenant filter and soft delete filter
        var templates = await _dbContext.Set<EmailTemplate>()
            .IgnoreQueryFilters()
            .Where(t => t.Name == templateName && t.IsActive && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        if (templates.Count == 0)
            return null;

        // Prefer tenant-specific template, fall back to platform template (TenantId = null)
        var tenantTemplate = templates.FirstOrDefault(t => t.TenantId == currentTenantId);
        if (tenantTemplate != null)
        {
            _logger.LogDebug("Using tenant-specific email template '{TemplateName}' for tenant '{TenantId}'", templateName, currentTenantId);
            return tenantTemplate;
        }

        var platformTemplate = templates.FirstOrDefault(t => t.TenantId == null);
        if (platformTemplate != null)
        {
            _logger.LogDebug("Using platform-level email template '{TemplateName}' (no tenant-specific override)", templateName);
            return platformTemplate;
        }

        // If we have templates but none match current tenant or platform, log warning
        _logger.LogWarning("Email template '{TemplateName}' exists but not for tenant '{TenantId}' or platform level", templateName, currentTenantId);
        return null;
    }

    /// <summary>
    /// Replaces {{placeholder}} tokens with values from the model.
    /// </summary>
    private static string ReplacePlaceholders<T>(string template, T model)
    {
        if (string.IsNullOrEmpty(template) || model == null)
            return template;

        var result = template;
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var placeholder = $"{{{{{prop.Name}}}}}"; // {{PropertyName}}
            var value = prop.GetValue(model)?.ToString() ?? string.Empty;
            result = result.Replace(placeholder, value);
        }

        return result;
    }
}
