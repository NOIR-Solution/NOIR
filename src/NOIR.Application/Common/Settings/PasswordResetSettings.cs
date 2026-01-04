namespace NOIR.Application.Common.Settings;

/// <summary>
/// Configuration settings for password reset functionality.
/// </summary>
public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";

    /// <summary>
    /// Length of the OTP code (default: 6 digits).
    /// </summary>
    [Range(4, 10, ErrorMessage = "OtpLength must be between 4 and 10")]
    public int OtpLength { get; set; } = 6;

    /// <summary>
    /// OTP expiry time in minutes (default: 5).
    /// </summary>
    [Range(1, 30, ErrorMessage = "OtpExpiryMinutes must be between 1 and 30")]
    public int OtpExpiryMinutes { get; set; } = 5;

    /// <summary>
    /// Cooldown between OTP resends in seconds (default: 60).
    /// </summary>
    [Range(30, 300, ErrorMessage = "ResendCooldownSeconds must be between 30 and 300")]
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of OTP resends per session (default: 3).
    /// </summary>
    [Range(1, 10, ErrorMessage = "MaxResendCount must be between 1 and 10")]
    public int MaxResendCount { get; set; } = 3;

    /// <summary>
    /// Maximum OTP requests per email per hour (default: 3).
    /// Used for rate limiting to prevent abuse.
    /// </summary>
    [Range(1, 20, ErrorMessage = "MaxRequestsPerEmailPerHour must be between 1 and 20")]
    public int MaxRequestsPerEmailPerHour { get; set; } = 3;

    /// <summary>
    /// Reset token expiry time in minutes after OTP verification (default: 15).
    /// </summary>
    [Range(5, 60, ErrorMessage = "ResetTokenExpiryMinutes must be between 5 and 60")]
    public int ResetTokenExpiryMinutes { get; set; } = 15;
}
