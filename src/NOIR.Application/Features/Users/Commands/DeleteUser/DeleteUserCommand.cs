namespace NOIR.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to soft-delete a user (disable account).
/// </summary>
public sealed record DeleteUserCommand(string UserId) : IAuditableCommand
{
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => UserId;
}
