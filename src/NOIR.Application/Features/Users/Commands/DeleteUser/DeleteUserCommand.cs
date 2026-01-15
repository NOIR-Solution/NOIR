namespace NOIR.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to soft-delete a user (disable account).
/// </summary>
public sealed record DeleteUserCommand(string UserId, string? UserEmail = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => UserEmail;
    public string? GetActionDescription() => UserEmail != null
        ? $"Deleted user '{UserEmail}'"
        : "Deleted user";
}
