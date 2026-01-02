namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Configuration settings for ASP.NET Core Identity.
/// Supports environment-specific password policies via appsettings.
/// Production: Strong policy (12+ chars, complexity requirements)
/// Development: Simple policy (6 chars, no complexity)
/// </summary>
public class IdentitySettings
{
    public const string SectionName = "Identity";

    public PasswordSettings Password { get; set; } = new();
    public LockoutSettings Lockout { get; set; } = new();
}

public class PasswordSettings
{
    /// <summary>
    /// Require at least one digit (0-9).
    /// Default: true (production), false (development)
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Require at least one lowercase letter (a-z).
    /// Default: true (production), false (development)
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require at least one uppercase letter (A-Z).
    /// Default: true (production), false (development)
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require at least one non-alphanumeric character (!@#$%^&amp;* etc).
    /// Default: true (production), false (development)
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = true;

    /// <summary>
    /// Minimum password length.
    /// Default: 12 (production), 6 (development)
    /// </summary>
    public int RequiredLength { get; set; } = 12;

    /// <summary>
    /// Minimum number of unique characters in password.
    /// Default: 4 (production), 1 (development)
    /// </summary>
    public int RequiredUniqueChars { get; set; } = 4;
}

public class LockoutSettings
{
    /// <summary>
    /// How long a user is locked out after too many failed attempts.
    /// Default: 15 minutes (production), 5 minutes (development)
    /// </summary>
    public int DefaultLockoutTimeSpanMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum failed login attempts before lockout.
    /// Default: 5 (production), 10 (development)
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Whether new users can be locked out.
    /// Default: true
    /// </summary>
    public bool AllowedForNewUsers { get; set; } = true;
}
