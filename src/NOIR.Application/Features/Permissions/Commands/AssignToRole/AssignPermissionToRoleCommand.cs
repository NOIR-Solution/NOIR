namespace NOIR.Application.Features.Permissions.Commands.AssignToRole;

/// <summary>
/// Command to assign permissions to a role.
/// </summary>
public sealed record AssignPermissionToRoleCommand(
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
            ? $"Assigned {permText} to role '{RoleName}'"
            : $"Assigned {permText} to role";
    }
}
