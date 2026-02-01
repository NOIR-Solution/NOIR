namespace NOIR.Infrastructure.Services;

/// <summary>
/// Email service implementation.
/// SMTP fallback chain: Tenant DB settings → Platform DB settings → appsettings.json (FluentEmail).
/// Uses database templates for email content with tenant override support.
/// </summary>
public class EmailService : IEmailService, IScopedService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly IRepository<EmailTemplate, Guid> _emailTemplateRepository;
    private readonly IOptionsMonitor<EmailSettings> _emailSettings;
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor<Tenant> _tenantContextAccessor;
    private readonly IFusionCache _cache;
    private readonly ILogger<EmailService> _logger;

    // Cache durations
    private static readonly TimeSpan TemplateCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SmtpSettingsCacheDuration = TimeSpan.FromMinutes(5);

    public EmailService(
        IFluentEmail fluentEmail,
        IRepository<EmailTemplate, Guid> emailTemplateRepository,
        IOptionsMonitor<EmailSettings> emailSettings,
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor<Tenant> tenantContextAccessor,
        IFusionCache cache,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail;
        _emailTemplateRepository = emailTemplateRepository;
        _emailSettings = emailSettings;
        _settingsService = settingsService;
        _tenantContextAccessor = tenantContextAccessor;
        _cache = cache;
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
    /// Tries to send email using database-configured SMTP settings with fallback chain:
    /// 1. Tenant-specific SMTP settings (if tenant context exists)
    /// 2. Platform-level SMTP settings (tenantId = null)
    /// Returns null if no DB settings are configured (caller should fallback to FluentEmail/appsettings).
    /// Returns true/false if DB settings exist and send was attempted.
    /// </summary>
    private async Task<bool?> TrySendViaDbSettingsAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string body,
        bool isHtml,
        CancellationToken cancellationToken)
    {
        // Get SMTP settings with fallback chain: Tenant → Platform
        var settings = await GetSmtpSettingsWithFallbackAsync(cancellationToken);

        // If no DB settings at any level, return null to signal fallback to FluentEmail
        if (settings == null)
            return null;

        var (host, port, username, password, useSsl, fromEmail, fromName, source) = settings.Value;

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

            _logger.LogDebug("Email sent via {Source} SMTP to {Recipients}", source, string.Join(", ", recipients));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via {Source} SMTP ({Host}:{Port})", source, host, port);
            return false;
        }
    }

    /// <summary>
    /// Gets SMTP settings with fallback chain: Tenant → Platform.
    /// Returns null if no SMTP settings configured at any level.
    /// Uses FusionCache for performance optimization.
    /// </summary>
    private async Task<(string Host, int Port, string? Username, string? Password, bool UseSsl, string FromEmail, string FromName, string Source)?> GetSmtpSettingsWithFallbackAsync(
        CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var cacheKey = CacheKeys.SmtpSettings(currentTenantId);

        var cached = await _cache.GetOrSetAsync(
            cacheKey,
            async token => await FetchSmtpSettingsFromDatabaseAsync(currentTenantId, token),
            options => options
                .SetDuration(SmtpSettingsCacheDuration)
                .SetFailSafe(true, TimeSpan.FromHours(1)),
            cancellationToken);

        return cached?.ToTuple();
    }

    /// <summary>
    /// Fetches SMTP settings from database with tenant fallback logic.
    /// </summary>
    private async Task<SmtpSettingsCache?> FetchSmtpSettingsFromDatabaseAsync(string? currentTenantId, CancellationToken cancellationToken)
    {
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
                    _logger.LogDebug("Using tenant-specific SMTP settings for tenant {TenantId}", currentTenantId);
                    return SmtpSettingsCache.FromTuple(ParseSmtpSettings(tenantSettings, "Tenant"));
                }
            }
        }

        // Step 2: Check for platform-level SMTP settings
        var platformSettings = await _settingsService.GetSettingsAsync(
            tenantId: null,
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        if (platformSettings.Count > 0 && platformSettings.ContainsKey("smtp:host"))
        {
            var platformHost = platformSettings.GetValueOrDefault("smtp:host", string.Empty);
            if (!string.IsNullOrWhiteSpace(platformHost))
            {
                _logger.LogDebug("Using platform-level SMTP settings (no tenant override)");
                return SmtpSettingsCache.FromTuple(ParseSmtpSettings(platformSettings, "Platform"));
            }
        }

        // Step 3: No DB settings - return null to fall back to FluentEmail (appsettings.json)
        return null;
    }

    /// <summary>
    /// Parses SMTP settings from a dictionary into a typed tuple.
    /// </summary>
    private (string Host, int Port, string? Username, string? Password, bool UseSsl, string FromEmail, string FromName, string Source)
        ParseSmtpSettings(IReadOnlyDictionary<string, string> settings, string source)
    {
        var host = settings.GetValueOrDefault("smtp:host", string.Empty)!;
        var port = int.TryParse(settings.GetValueOrDefault("smtp:port"), out var p) ? p : 25;
        var username = settings.GetValueOrDefault("smtp:username");
        var password = settings.GetValueOrDefault("smtp:password");
        var useSsl = bool.TryParse(settings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl;
        var fromEmail = settings.GetValueOrDefault("smtp:from_email", _emailSettings.CurrentValue.DefaultFromEmail)!;
        var fromName = settings.GetValueOrDefault("smtp:from_name", _emailSettings.CurrentValue.DefaultFromName)!;

        return (host, port, username, password, useSsl, fromEmail, fromName, source);
    }

    /// <summary>
    /// Gets an email template with fallback logic:
    /// 1. First tries to find tenant-specific template (TenantId = current tenant)
    /// 2. Falls back to platform-level template (TenantId = null)
    /// This allows tenants to override platform defaults while sharing common templates.
    /// Uses FusionCache for performance optimization.
    /// </summary>
    private async Task<EmailTemplate?> GetTemplateWithFallbackAsync(string templateName, CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var cacheKey = CacheKeys.EmailTemplate(templateName, currentTenantId);

        return await _cache.GetOrSetAsync(
            cacheKey,
            async token => await FetchTemplateFromDatabaseAsync(templateName, currentTenantId, token),
            options => options
                .SetDuration(TemplateCacheDuration)
                .SetFailSafe(true, TimeSpan.FromHours(1)),
            cancellationToken);
    }

    /// <summary>
    /// Fetches email template from database with tenant fallback logic.
    /// </summary>
    private async Task<EmailTemplate?> FetchTemplateFromDatabaseAsync(string templateName, string? currentTenantId, CancellationToken cancellationToken)
    {
        // Query all templates with this name using specification (ignores tenant and soft delete filters)
        var spec = new EmailTemplateByNameWithFallbackSpec(templateName);
        var templates = await _emailTemplateRepository.ListAsync(spec, cancellationToken);

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

/// <summary>
/// Cache-friendly wrapper for SMTP settings.
/// FusionCache requires a reference type for nullable caching scenarios.
/// </summary>
internal sealed record SmtpSettingsCache(
    string Host,
    int Port,
    string? Username,
    string? Password,
    bool UseSsl,
    string FromEmail,
    string FromName,
    string Source)
{
    public (string Host, int Port, string? Username, string? Password, bool UseSsl, string FromEmail, string FromName, string Source) ToTuple()
        => (Host, Port, Username, Password, UseSsl, FromEmail, FromName, Source);

    public static SmtpSettingsCache FromTuple((string Host, int Port, string? Username, string? Password, bool UseSsl, string FromEmail, string FromName, string Source) tuple)
        => new(tuple.Host, tuple.Port, tuple.Username, tuple.Password, tuple.UseSsl, tuple.FromEmail, tuple.FromName, tuple.Source);
}
