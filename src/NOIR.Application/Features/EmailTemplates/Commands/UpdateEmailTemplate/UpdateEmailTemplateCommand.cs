namespace NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Command to update an email template's content.
/// </summary>
public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    string? Description,
    string? TemplateName = null) : IAuditableCommand<EmailTemplateDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => TemplateName ?? Subject;
    public string? GetActionDescription() => $"Updated email template '{GetTargetDisplayName()}'";
}
