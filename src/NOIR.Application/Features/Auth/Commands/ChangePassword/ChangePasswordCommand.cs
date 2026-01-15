namespace NOIR.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Result of a password change operation.
/// </summary>
public sealed record ChangePasswordResult(bool Success, string Message);

/// <summary>
/// Command to change the authenticated user's password.
/// Requires current password verification for security.
/// All sessions are revoked after successful password change.
/// </summary>
/// <param name="CurrentPassword">The user's current password for verification.</param>
/// <param name="NewPassword">The new password to set.</param>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IAuditableCommand<ChangePasswordResult>
{
    /// <summary>
    /// User ID changing password. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetActionDescription() => "Changed password";
}
