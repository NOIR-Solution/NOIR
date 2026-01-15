using NOIR.Application.Features.Auth.Queries.GetUserById;

namespace NOIR.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command for admin to update any user.
/// </summary>
public sealed record UpdateUserCommand(
    string UserId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    bool? LockoutEnabled) : IAuditableCommand<UserProfileDto>
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => DisplayName ?? $"{FirstName} {LastName}".Trim();
    public string? GetActionDescription() => $"Updated user '{GetTargetDisplayName()}'";
}
