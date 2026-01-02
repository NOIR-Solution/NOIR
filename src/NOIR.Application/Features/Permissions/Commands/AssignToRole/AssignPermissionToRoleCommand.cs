namespace NOIR.Application.Features.Permissions.Commands.AssignToRole;

/// <summary>
/// Command to assign permissions to a role.
/// </summary>
public sealed record AssignPermissionToRoleCommand(
    string RoleId,
    IReadOnlyList<string> Permissions) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
}
