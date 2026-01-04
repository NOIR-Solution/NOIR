namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing the password reset flow with OTP verification.
/// Handles OTP generation, verification, and password reset token management.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Initiates a password reset request by generating and sending an OTP.
    /// </summary>
    /// <param name="email">The email address to send the OTP to.</param>
    /// <param name="tenantId">The tenant ID for multi-tenancy.</param>
    /// <param name="ipAddress">The IP address of the requester for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing session token and masked email, or error if rate limited.</returns>
    Task<Result<PasswordResetRequestResult>> RequestPasswordResetAsync(
        string email,
        string? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an OTP and generates a reset token if valid.
    /// </summary>
    /// <param name="sessionToken">The session token from the request step.</param>
    /// <param name="otp">The OTP code entered by the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing reset token if OTP is valid, or error details.</returns>
    Task<Result<PasswordResetVerifyResult>> VerifyOtpAsync(
        string sessionToken,
        string otp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends the OTP for an existing password reset session.
    /// </summary>
    /// <param name="sessionToken">The session token from the original request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing next resend time and remaining attempts, or error if cooldown active.</returns>
    Task<Result<PasswordResetResendResult>> ResendOtpAsync(
        string sessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    /// <param name="resetToken">The reset token from the verify step.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ResetPasswordAsync(
        string resetToken,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the rate limit status for an email address.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rate limited, false if request is allowed.</returns>
    Task<bool> IsRateLimitedAsync(
        string email,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a password reset request operation.
/// </summary>
public record PasswordResetRequestResult(
    string SessionToken,
    string MaskedEmail,
    DateTimeOffset ExpiresAt,
    int OtpLength);

/// <summary>
/// Result of OTP verification.
/// </summary>
public record PasswordResetVerifyResult(
    string ResetToken,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Result of OTP resend operation.
/// </summary>
public record PasswordResetResendResult(
    bool Success,
    DateTimeOffset? NextResendAt,
    int RemainingResends);
