namespace NOIR.Application.Features.Users.Commands.LockUser;

/// <summary>
/// Command to lock or unlock a user account.
/// When locked, the user cannot sign in.
/// </summary>
public sealed record LockUserCommand(string UserId, bool Lock, string? UserEmail = null) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => UserEmail;
    public string? GetActionDescription() => Lock
        ? (UserEmail != null ? $"Locked user '{UserEmail}'" : "Locked user")
        : (UserEmail != null ? $"Unlocked user '{UserEmail}'" : "Unlocked user");
}
