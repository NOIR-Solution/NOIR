namespace NOIR.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to soft-delete a user (disable account).
/// </summary>
public sealed record DeleteUserCommand(string TargetUserId, string? UserEmail = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => TargetUserId;
    public string? GetTargetDisplayName() => UserEmail;
    public string? GetActionDescription() => UserEmail != null
        ? $"Deleted user '{UserEmail}'"
        : "Deleted user";
}
