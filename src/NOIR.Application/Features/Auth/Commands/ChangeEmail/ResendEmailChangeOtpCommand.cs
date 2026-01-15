namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Command to resend an email change OTP.
/// </summary>
public sealed record ResendEmailChangeOtpCommand(
    string SessionToken) : IAuditableCommand<EmailChangeResendResult>
{
    /// <summary>
    /// User ID for audit. Set by the endpoint from current user context.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;

    public AuditOperationType OperationType => AuditOperationType.Update;

    public string? GetActionDescription() => "Resent email change OTP";
}
