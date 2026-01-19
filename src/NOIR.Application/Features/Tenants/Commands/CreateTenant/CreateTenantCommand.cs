namespace NOIR.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant.
/// </summary>
public sealed record CreateTenantCommand(
    string Identifier,
    string Name,
    string? Domain = null,
    string? Description = null,
    string? Note = null,
    bool IsActive = true) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Identifier;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created tenant '{Name}'";
}
