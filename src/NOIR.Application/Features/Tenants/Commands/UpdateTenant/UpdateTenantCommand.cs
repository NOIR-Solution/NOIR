
namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Request body for updating a tenant (excludes TenantId which comes from URL).
/// </summary>
public sealed record UpdateTenantRequest(
    string Identifier,
    string Name,
    string? Domain = null,
    string? Description = null,
    string? Note = null,
    bool IsActive = true);

/// <summary>
/// Command to update an existing tenant.
/// Implements IAuditableCommand&lt;TenantDto&gt; to enable before/after diff tracking.
/// </summary>
public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Identifier,
    string Name,
    string? Domain = null,
    string? Description = null,
    string? Note = null,
    bool IsActive = true) : IAuditableCommand<TenantDto>
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TenantId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated tenant '{Name}'";

    /// <summary>
    /// Creates a command from a request body and tenant ID from URL.
    /// </summary>
    public static UpdateTenantCommand FromRequest(Guid tenantId, UpdateTenantRequest request) =>
        new(tenantId, request.Identifier, request.Name, request.Domain, request.Description, request.Note, request.IsActive);
}
