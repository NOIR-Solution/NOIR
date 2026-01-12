namespace NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Command to update an email template's content.
/// </summary>
public sealed record UpdateEmailTemplateCommand(
    Guid Id,
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    string? Description) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
}
