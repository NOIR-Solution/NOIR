namespace NOIR.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command for admin to update any user.
/// </summary>
public sealed record UpdateUserCommand(
    string UserId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    bool? LockoutEnabled) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => UserId;
}
