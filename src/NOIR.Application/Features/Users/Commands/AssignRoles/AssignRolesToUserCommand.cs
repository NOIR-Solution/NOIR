namespace NOIR.Application.Features.Users.Commands.AssignRoles;

/// <summary>
/// Command to assign roles to a user (replaces existing roles).
/// </summary>
public sealed record AssignRolesToUserCommand(
    string UserId,
    IReadOnlyList<string> RoleNames,
    string? UserEmail = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => UserEmail;
    public string? GetActionDescription()
    {
        var rolesText = RoleNames.Count switch
        {
            0 => "no roles",
            1 => $"role '{RoleNames[0]}'",
            _ => $"{RoleNames.Count} roles"
        };
        return UserEmail != null
            ? $"Assigned {rolesText} to user '{UserEmail}'"
            : $"Assigned {rolesText} to user";
    }
}
