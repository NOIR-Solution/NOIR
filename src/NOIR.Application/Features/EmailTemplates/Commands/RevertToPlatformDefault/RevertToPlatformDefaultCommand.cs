namespace NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;

/// <summary>
/// Command to revert a tenant's customized email template to the platform default.
/// This deletes the tenant's custom version, making the platform template visible again.
/// </summary>
public sealed record RevertToPlatformDefaultCommand(Guid Id) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Email template";
    public string? GetActionDescription() => "Reverted email template to platform default";
}
