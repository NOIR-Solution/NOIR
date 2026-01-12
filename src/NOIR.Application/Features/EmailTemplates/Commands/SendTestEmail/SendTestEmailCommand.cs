namespace NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;

/// <summary>
/// Command to send a test email using a template.
/// </summary>
public sealed record SendTestEmailCommand(
    Guid TemplateId,
    string RecipientEmail,
    Dictionary<string, string> SampleData);
