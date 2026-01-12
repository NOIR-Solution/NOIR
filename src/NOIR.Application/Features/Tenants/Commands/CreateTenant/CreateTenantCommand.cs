namespace NOIR.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant.
/// </summary>
public sealed record CreateTenantCommand(
    string Identifier,
    string Name,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? AccentColor = null,
    string? Theme = null,
    bool IsActive = true) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Identifier;
}
