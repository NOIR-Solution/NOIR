namespace NOIR.Application.Features.Permissions.Commands.RemoveFromRole;

/// <summary>
/// Command to remove permissions from a role.
/// </summary>
public sealed record RemovePermissionFromRoleCommand(
    string RoleId,
    IReadOnlyList<string> Permissions,
    string? RoleName = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => RoleId;
    public string? GetTargetDisplayName() => RoleName;
    public string? GetActionDescription()
    {
        var permText = Permissions.Count switch
        {
            0 => "no permissions",
            1 => $"permission '{Permissions[0]}'",
            _ => $"{Permissions.Count} permissions"
        };
        return RoleName != null
            ? $"Removed {permText} from role '{RoleName}'"
            : $"Removed {permText} from role";
    }
}
