namespace NOIR.Domain.Entities;

/// <summary>
/// Entity for managing password reset OTP (One-Time Password) lifecycle.
/// Implements session token binding to prevent bypass attacks across browsers/sessions.
/// Uses bcrypt hashing for OTP storage security.
/// </summary>
public class PasswordResetOtp : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// The user ID this OTP belongs to (nullable for non-existent emails).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// The email address this OTP was sent to.
    /// Used for rate limiting per email across all sessions.
    /// </summary>
    public string Email { get; private set; } = default!;

    /// <summary>
    /// Bcrypt hash of the OTP for secure storage.
    /// </summary>
    public string OtpHash { get; private set; } = default!;

    /// <summary>
    /// Session token binding the email to this reset flow.
    /// Prevents users from bypassing cooldown by starting new sessions.
    /// </summary>
    public string SessionToken { get; private set; } = default!;

    /// <summary>
    /// When this OTP expires (default: 5 minutes from creation).
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Whether this OTP has been successfully used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// When the OTP was successfully verified.
    /// </summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>
    /// Number of failed verification attempts.
    /// Tracked for security monitoring (no limit enforced per user requirement).
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// IP address that initiated the password reset request.
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// Reset token issued after successful OTP verification.
    /// One-time use, short expiry (15 minutes).
    /// </summary>
    public string? ResetToken { get; private set; }

    /// <summary>
    /// When the reset token expires.
    /// </summary>
    public DateTimeOffset? ResetTokenExpiresAt { get; private set; }

    /// <summary>
    /// Number of times OTP has been resent for this session.
    /// </summary>
    public int ResendCount { get; private set; }

    /// <summary>
    /// When the last OTP resend occurred.
    /// Used to enforce cooldown period.
    /// </summary>
    public DateTimeOffset? LastResendAt { get; private set; }

    /// <summary>
    /// Whether this OTP has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Whether this OTP is still valid (not expired and not used).
    /// </summary>
    public bool IsValid => !IsExpired && !IsUsed;

    /// <summary>
    /// Whether the reset token is still valid.
    /// </summary>
    public bool IsResetTokenValid =>
        ResetToken != null &&
        ResetTokenExpiresAt.HasValue &&
        DateTimeOffset.UtcNow < ResetTokenExpiresAt.Value &&
        !IsDeleted;

    // Private constructor for EF Core
    private PasswordResetOtp() : base() { }

    /// <summary>
    /// Creates a new password reset OTP.
    /// </summary>
    /// <param name="email">The email address requesting password reset.</param>
    /// <param name="otpHash">Bcrypt hash of the OTP.</param>
    /// <param name="sessionToken">Pre-generated cryptographically secure session token.</param>
    /// <param name="expiryMinutes">OTP expiry time in minutes.</param>
    /// <param name="userId">User ID if email exists in system (null for non-existent emails).</param>
    /// <param name="tenantId">Tenant ID for multi-tenancy.</param>
    /// <param name="ipAddress">IP address of the requester.</param>
    public static PasswordResetOtp Create(
        string email,
        string otpHash,
        string sessionToken,
        int expiryMinutes,
        string? userId = null,
        string? tenantId = null,
        string? ipAddress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionToken);

        return new PasswordResetOtp
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            OtpHash = otpHash,
            SessionToken = sessionToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
            UserId = userId,
            CreatedByIp = ipAddress,
            IsUsed = false,
            AttemptCount = 0,
            ResendCount = 0,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Records a failed OTP verification attempt.
    /// </summary>
    public void RecordFailedAttempt()
    {
        AttemptCount++;
    }

    /// <summary>
    /// Marks the OTP as successfully used and sets the reset token.
    /// </summary>
    /// <param name="resetToken">Pre-generated cryptographically secure reset token.</param>
    /// <param name="resetTokenExpiryMinutes">Reset token expiry time in minutes.</param>
    public void MarkAsUsed(string resetToken, int resetTokenExpiryMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);

        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
        ResetToken = resetToken;
        ResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(resetTokenExpiryMinutes);
    }

    /// <summary>
    /// Checks if a resend is allowed based on cooldown and max resend count.
    /// </summary>
    /// <param name="cooldownSeconds">Cooldown period in seconds between resends.</param>
    /// <param name="maxResendCount">Maximum number of resends allowed.</param>
    /// <returns>True if resend is allowed, false otherwise.</returns>
    public bool CanResend(int cooldownSeconds, int maxResendCount)
    {
        if (ResendCount >= maxResendCount)
            return false;

        if (!LastResendAt.HasValue)
            return true;

        var cooldownEnd = LastResendAt.Value.AddSeconds(cooldownSeconds);
        return DateTimeOffset.UtcNow >= cooldownEnd;
    }

    /// <summary>
    /// Gets the remaining cooldown time in seconds before next resend is allowed.
    /// </summary>
    /// <param name="cooldownSeconds">Cooldown period in seconds between resends.</param>
    /// <returns>Remaining seconds, or 0 if cooldown has passed.</returns>
    public int GetRemainingCooldownSeconds(int cooldownSeconds)
    {
        if (!LastResendAt.HasValue)
            return 0;

        var cooldownEnd = LastResendAt.Value.AddSeconds(cooldownSeconds);
        var remaining = (cooldownEnd - DateTimeOffset.UtcNow).TotalSeconds;
        return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
    }

    /// <summary>
    /// Resends the OTP with a new hash and extended expiry.
    /// </summary>
    /// <param name="newOtpHash">Bcrypt hash of the new OTP.</param>
    /// <param name="expiryMinutes">New OTP expiry time in minutes.</param>
    public void Resend(string newOtpHash, int expiryMinutes)
    {
        OtpHash = newOtpHash;
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        ResendCount++;
        LastResendAt = DateTimeOffset.UtcNow;
        AttemptCount = 0; // Reset attempt count on resend
    }

    /// <summary>
    /// Invalidates the reset token after password change.
    /// </summary>
    public void InvalidateResetToken()
    {
        ResetToken = null;
        ResetTokenExpiresAt = null;
    }
}
