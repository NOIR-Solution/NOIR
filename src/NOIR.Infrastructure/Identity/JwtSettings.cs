namespace NOIR.Infrastructure.Identity;

/// <summary>
/// JWT configuration settings with validation.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JWT Secret is required")]
    [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters")]
    public string Secret { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes (default: 15 for security).
    /// </summary>
    [Range(1, 1440, ErrorMessage = "ExpirationInMinutes must be between 1 and 1440")]
    public int ExpirationInMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration in days.
    /// </summary>
    [Range(1, 365, ErrorMessage = "RefreshTokenExpirationInDays must be between 1 and 365")]
    public int RefreshTokenExpirationInDays { get; set; } = 7;

    /// <summary>
    /// Whether to enable device fingerprinting for token binding.
    /// </summary>
    public bool EnableDeviceFingerprinting { get; set; } = true;

    /// <summary>
    /// Maximum concurrent sessions per user (0 = unlimited).
    /// </summary>
    [Range(0, 100, ErrorMessage = "MaxConcurrentSessions must be between 0 and 100")]
    public int MaxConcurrentSessions { get; set; } = 5;

    /// <summary>
    /// Days to keep expired/revoked tokens for audit purposes.
    /// </summary>
    [Range(1, 365, ErrorMessage = "TokenRetentionDays must be between 1 and 365")]
    public int TokenRetentionDays { get; set; } = 30;
}
