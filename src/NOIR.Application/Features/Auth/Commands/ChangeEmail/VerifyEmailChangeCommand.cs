namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Command to verify an email change OTP and complete the email update.
/// </summary>
public sealed record VerifyEmailChangeCommand(
    string SessionToken,
    string Otp) : IAuditableCommand<EmailChangeVerifyResult>
{
    /// <summary>
    /// User ID for audit. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;
}
