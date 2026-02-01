namespace NOIR.Domain.Events.Auth;

/// <summary>
/// Raised when a refresh token is created for a user.
/// </summary>
public sealed record RefreshTokenCreatedEvent(
    Guid TokenId,
    string UserId,
    string? DeviceInfo) : DomainEvent;

/// <summary>
/// Raised when a refresh token is revoked.
/// </summary>
public sealed record RefreshTokenRevokedEvent(
    Guid TokenId,
    string UserId,
    string? Reason) : DomainEvent;

/// <summary>
/// Raised when a password reset OTP is created.
/// </summary>
public sealed record PasswordResetOtpCreatedEvent(
    Guid OtpId,
    string UserId,
    string MaskedEmail) : DomainEvent;

/// <summary>
/// Raised when a password reset OTP is verified successfully.
/// </summary>
public sealed record PasswordResetOtpVerifiedEvent(
    Guid OtpId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a password reset OTP verification fails.
/// </summary>
public sealed record PasswordResetOtpFailedEvent(
    Guid OtpId,
    string UserId,
    int AttemptCount) : DomainEvent;

/// <summary>
/// Raised when a password reset OTP is resent.
/// </summary>
public sealed record PasswordResetOtpResentEvent(
    Guid OtpId,
    string UserId) : DomainEvent;
