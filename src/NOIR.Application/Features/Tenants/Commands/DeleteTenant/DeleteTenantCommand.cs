namespace NOIR.Application.Features.Tenants.Commands.DeleteTenant;

/// <summary>
/// Command to delete (soft delete) a tenant.
/// </summary>
public sealed record DeleteTenantCommand(Guid TenantId) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => TenantId;
}
