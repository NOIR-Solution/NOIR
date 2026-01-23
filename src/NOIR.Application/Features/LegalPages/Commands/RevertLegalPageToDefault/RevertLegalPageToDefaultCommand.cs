namespace NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;

/// <summary>
/// Command to revert a tenant's customized legal page to the platform default.
/// This deletes the tenant's custom version, making the platform page visible again.
/// </summary>
public sealed record RevertLegalPageToDefaultCommand(Guid Id) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Legal page";
    public string? GetActionDescription() => "Reverted legal page to platform default";
}
