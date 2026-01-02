namespace NOIR.Application.Features.Permissions.Commands.RemoveFromRole;

/// <summary>
/// Command to remove permissions from a role.
/// </summary>
public sealed record RemovePermissionFromRoleCommand(
    string RoleId,
    IReadOnlyList<string> Permissions) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
}
