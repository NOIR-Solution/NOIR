namespace NOIR.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Command to delete a role.
/// </summary>
public sealed record DeleteRoleCommand(string RoleId) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => RoleId;
}
