namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing the email change flow with OTP verification.
/// Similar to IPasswordResetService but for changing user email.
/// </summary>
public interface IEmailChangeService
{
    /// <summary>
    /// Initiates an email change request by generating and sending an OTP to the new email.
    /// </summary>
    /// <param name="userId">The user ID requesting the change.</param>
    /// <param name="newEmail">The new email address to change to.</param>
    /// <param name="tenantId">The tenant ID for multi-tenancy.</param>
    /// <param name="ipAddress">The IP address of the requester for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing session token and masked email, or error if rate limited.</returns>
    Task<Result<EmailChangeRequestResult>> RequestEmailChangeAsync(
        string userId,
        string newEmail,
        string? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an OTP and completes the email change if valid.
    /// </summary>
    /// <param name="sessionToken">The session token from the request step.</param>
    /// <param name="otp">The OTP code entered by the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the new email if OTP is valid, or error details.</returns>
    Task<Result<EmailChangeVerifyResult>> VerifyOtpAsync(
        string sessionToken,
        string otp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends the OTP for an existing email change session.
    /// </summary>
    /// <param name="sessionToken">The session token from the original request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing next resend time and remaining attempts, or error if cooldown active.</returns>
    Task<Result<EmailChangeResendResult>> ResendOtpAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the rate limit status for a user's email change requests.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rate limited, false if request is allowed.</returns>
    Task<bool> IsRateLimitedAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an email change request operation.
/// </summary>
public record EmailChangeRequestResult(
    string SessionToken,
    string MaskedEmail,
    DateTimeOffset ExpiresAt,
    int OtpLength);

/// <summary>
/// Result of email change OTP verification.
/// </summary>
public record EmailChangeVerifyResult(
    string NewEmail,
    string Message);

/// <summary>
/// Result of OTP resend operation.
/// </summary>
public record EmailChangeResendResult(
    bool Success,
    DateTimeOffset? NextResendAt,
    int RemainingResends);
