namespace NOIR.Application.Features.Tenants.Commands.DeleteTenant;

/// <summary>
/// Command to delete (soft delete) a tenant.
/// </summary>
public sealed record DeleteTenantCommand(Guid TenantId, string? TenantName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => TenantId;
    public string? GetTargetDisplayName() => TenantName;
    public string? GetActionDescription() => TenantName != null
        ? $"Deleted tenant '{TenantName}'"
        : "Deleted tenant";
}
