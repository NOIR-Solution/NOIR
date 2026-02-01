namespace NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

/// <summary>
/// Command to toggle an email template's active status.
/// </summary>
public sealed record ToggleEmailTemplateActiveCommand(
    Guid Id,
    bool IsActive,
    string? TemplateName = null) : IAuditableCommand<EmailTemplateDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => TemplateName;
    public string? GetActionDescription() => IsActive
        ? $"Activated email template '{TemplateName}'"
        : $"Deactivated email template '{TemplateName}'";
}
