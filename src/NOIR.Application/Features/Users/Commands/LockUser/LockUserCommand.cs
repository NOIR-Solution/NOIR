namespace NOIR.Application.Features.Users.Commands.LockUser;

/// <summary>
/// Command to lock or unlock a user account.
/// When locked, the user cannot sign in.
/// </summary>
public sealed record LockUserCommand(string UserId, bool Lock) : IAuditableCommand
{
    public AuditOperationType OperationType => Lock ? AuditOperationType.Update : AuditOperationType.Update;
    public object? GetTargetId() => UserId;
}
