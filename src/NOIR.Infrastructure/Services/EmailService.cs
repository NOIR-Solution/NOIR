namespace NOIR.Infrastructure.Services;

/// <summary>
/// FluentEmail implementation of email service.
/// Uses database templates for email content.
/// </summary>
public class EmailService : IEmailService, IScopedService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly IReadRepository<EmailTemplate, Guid> _templateRepository;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IFluentEmail fluentEmail,
        IReadRepository<EmailTemplate, Guid> templateRepository,
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail;
        _templateRepository = templateRepository;
        _emailSettings = emailSettings.Value;
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
            // Load template from database by name
            var template = await _templateRepository.FirstOrDefaultAsync(
                new EmailTemplateByNameSpec(templateName),
                cancellationToken);

            if (template == null)
            {
                _logger.LogError("Email template '{TemplateName}' not found", templateName);
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
