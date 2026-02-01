namespace NOIR.Application.Features.Users.Commands.LockUser;

/// <summary>
/// Command to lock or unlock a user account.
/// When locked, the user cannot sign in.
/// </summary>
public sealed record LockUserCommand(string TargetUserId, bool Lock, string? UserEmail = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TargetUserId;
    public string? GetTargetDisplayName() => UserEmail;
    public string? GetActionDescription() => Lock
        ? (UserEmail != null ? $"Locked user '{UserEmail}'" : "Locked user")
        : (UserEmail != null ? $"Unlocked user '{UserEmail}'" : "Unlocked user");
}
