namespace NOIR.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant.
/// </summary>
public sealed record CreateTenantCommand(
    string Identifier,
    string Name,
    bool IsActive = true) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Identifier;
}
