namespace NOIR.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Command to create a new role.
/// </summary>
public sealed record CreateRoleCommand(
    string Name,
    IReadOnlyList<string>? Permissions = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Name;
}
