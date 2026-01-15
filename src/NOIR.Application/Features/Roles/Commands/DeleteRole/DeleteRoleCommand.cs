namespace NOIR.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Command to delete a role.
/// </summary>
public sealed record DeleteRoleCommand(string RoleId, string? RoleName = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => RoleId;
    public string? GetTargetDisplayName() => RoleName;
    public string? GetActionDescription() => RoleName != null
        ? $"Deleted role '{RoleName}'"
        : "Deleted role";
}
