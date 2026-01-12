namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Request body for updating a tenant (excludes TenantId which comes from URL).
/// </summary>
public sealed record UpdateTenantRequest(
    string Identifier,
    string Name,
    bool IsActive = true);

/// <summary>
/// Command to update an existing tenant.
/// </summary>
public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Identifier,
    string Name,
    bool IsActive = true) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TenantId;

    /// <summary>
    /// Creates a command from a request body and tenant ID from URL.
    /// </summary>
    public static UpdateTenantCommand FromRequest(Guid tenantId, UpdateTenantRequest request) =>
        new(tenantId, request.Identifier, request.Name, request.IsActive);
}
