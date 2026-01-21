namespace NOIR.Infrastructure.Services;

/// <summary>
/// FluentEmail implementation of email service.
/// Uses database templates for email content.
/// </summary>
public class EmailService : IEmailService, IScopedService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptionsMonitor<EmailSettings> _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IFluentEmail fluentEmail,
        ApplicationDbContext dbContext,
        IOptionsMonitor<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail;
        _dbContext = dbContext;
        _emailSettings = emailSettings;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
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
            var email = _fluentEmail
                .To(to.Select(x => new FluentEmail.Core.Models.Address(x)))
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
