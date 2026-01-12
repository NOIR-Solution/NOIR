namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Command to update an existing tenant.
/// Note: Identifier cannot be changed after creation.
/// </summary>
public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? AccentColor = null,
    string? Theme = null,
    bool IsActive = true) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TenantId;
}
