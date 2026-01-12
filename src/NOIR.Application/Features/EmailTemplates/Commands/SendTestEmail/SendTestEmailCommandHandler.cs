namespace NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;

/// <summary>
/// Wolverine handler for sending a test email using a template.
/// </summary>
public class SendTestEmailCommandHandler
{
    private readonly IRepository<EmailTemplate, Guid> _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendTestEmailCommandHandler> _logger;

    public SendTestEmailCommandHandler(
        IRepository<EmailTemplate, Guid> repository,
        IEmailService emailService,
        ILogger<SendTestEmailCommandHandler> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<EmailPreviewResponse>> Handle(
        SendTestEmailCommand command,
        CancellationToken cancellationToken)
    {
        // Get the template
        var spec = new EmailTemplateByIdSpec(command.TemplateId);
        var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailPreviewResponse>(
                Error.NotFound($"Email template with ID '{command.TemplateId}' not found.", "NOIR-EMAIL-001"));
        }

        // Replace variables in subject and body
        var subject = ReplaceVariables(template.Subject, command.SampleData);
        var htmlBody = ReplaceVariables(template.HtmlBody, command.SampleData);
        var plainTextBody = template.PlainTextBody is not null
            ? ReplaceVariables(template.PlainTextBody, command.SampleData)
            : null;

        // Send the test email
        try
        {
            var sendResult = await _emailService.SendAsync(
                command.RecipientEmail,
                subject,
                htmlBody,
                isHtml: true,
                cancellationToken);

            if (!sendResult)
            {
                _logger.LogError("Failed to send test email to {Email}",
                    command.RecipientEmail);

                return Result.Failure<EmailPreviewResponse>(
                    Error.Failure("NOIR-EMAIL-002", "Failed to send test email. Please check email configuration."));
            }

            _logger.LogInformation("Test email sent successfully to {Email} using template {TemplateName}",
                command.RecipientEmail, template.Name);

            return Result.Success(new EmailPreviewResponse(subject, htmlBody, plainTextBody));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending test email to {Email}", command.RecipientEmail);
            return Result.Failure<EmailPreviewResponse>(
                Error.Failure("NOIR-EMAIL-003", $"Error sending email: {ex.Message}"));
        }
    }

    private static string ReplaceVariables(string content, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            // Support both {{Variable}} and {Variable} formats
            content = content.Replace($"{{{{{key}}}}}", value);
            content = content.Replace($"{{{key}}}", value);
        }
        return content;
    }
}
