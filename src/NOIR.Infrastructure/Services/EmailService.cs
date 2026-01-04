namespace NOIR.Infrastructure.Services;

/// <summary>
/// FluentEmail implementation of email service.
/// </summary>
public class EmailService : IEmailService, IScopedService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IFluentEmail fluentEmail,
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail;
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
            // Build full template path - ensure it ends with .cshtml
            var templatePath = templateName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                ? templateName
                : $"{templateName}.cshtml";

            // Get the absolute templates path
            var templatesPath = Path.IsPathRooted(_emailSettings.TemplatesPath)
                ? _emailSettings.TemplatesPath
                : Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.TemplatesPath);

            var fullTemplatePath = Path.Combine(templatesPath, templatePath);

            var response = await _fluentEmail
                .To(to)
                .Subject(subject)
                .UsingTemplateFromFile(fullTemplatePath, model)
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
}
