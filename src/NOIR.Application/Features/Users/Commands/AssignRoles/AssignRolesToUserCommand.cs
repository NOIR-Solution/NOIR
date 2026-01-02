namespace NOIR.Application.Features.Users.Commands.AssignRoles;

/// <summary>
/// Command to assign roles to a user (replaces existing roles).
/// </summary>
public sealed record AssignRolesToUserCommand(
    string UserId,
    IReadOnlyList<string> RoleNames) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
}
