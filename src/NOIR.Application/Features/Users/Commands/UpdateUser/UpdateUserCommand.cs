namespace NOIR.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command for admin to update any user.
/// </summary>
public sealed record UpdateUserCommand(
    string TargetUserId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    bool? LockoutEnabled,
    string? UserEmail = null) : IAuditableCommand<UserProfileDto>
{
    /// <summary>
    /// The ID of the admin user performing the update (actor).
    /// Set by endpoint from ICurrentUser.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => TargetUserId;
    public string? GetTargetDisplayName() => UserEmail ?? DisplayName ?? $"{FirstName} {LastName}".Trim();
    public string? GetActionDescription() => $"Updated user '{GetTargetDisplayName()}'";
}
