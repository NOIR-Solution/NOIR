namespace NOIR.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Command to update a role's name.
/// </summary>
public sealed record UpdateRoleCommand(
    string RoleId,
    string Name) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
}
