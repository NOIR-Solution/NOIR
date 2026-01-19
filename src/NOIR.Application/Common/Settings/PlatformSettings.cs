namespace NOIR.Application.Common.Settings;

/// <summary>
/// Platform-level configuration settings for seeding and administration.
/// </summary>
public class PlatformSettings
{
    public const string SectionName = "Platform";

    /// <summary>
    /// Platform admin configuration (TenantId = null).
    /// This user has access to all tenants and platform-level operations.
    /// </summary>
    public PlatformAdminSettings PlatformAdmin { get; set; } = new();

    /// <summary>
    /// Default tenant configuration (created on first startup).
    /// </summary>
    public DefaultTenantSettings DefaultTenant { get; set; } = new();
}

/// <summary>
/// Platform admin user configuration.
/// </summary>
public class PlatformAdminSettings
{
    /// <summary>
    /// Email address for the platform admin user.
    /// </summary>
    [Required(ErrorMessage = "Platform admin email is required")]
    [EmailAddress(ErrorMessage = "Platform admin email must be a valid email address")]
    public string Email { get; set; } = "platform@noir.local";

    /// <summary>
    /// Password for the platform admin user.
    /// In production, use environment variables or secrets manager.
    /// </summary>
    [Required(ErrorMessage = "Platform admin password is required")]
    [MinLength(6, ErrorMessage = "Platform admin password must be at least 6 characters")]
    public string Password { get; set; } = "Platform123!";

    /// <summary>
    /// First name for the platform admin user.
    /// </summary>
    public string FirstName { get; set; } = "Platform";

    /// <summary>
    /// Last name for the platform admin user.
    /// </summary>
    public string LastName { get; set; } = "Administrator";
}

/// <summary>
/// Default tenant configuration (seeded on first startup).
/// </summary>
public class DefaultTenantSettings
{
    /// <summary>
    /// Whether to create a default tenant on startup.
    /// Set to false for platforms that only provision tenants via API.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Unique identifier for the default tenant.
    /// Used for tenant resolution and URLs.
    /// </summary>
    [Required(ErrorMessage = "Default tenant identifier is required")]
    [MinLength(2, ErrorMessage = "Default tenant identifier must be at least 2 characters")]
    public string Identifier { get; set; } = "default";

    /// <summary>
    /// Display name for the default tenant.
    /// </summary>
    [Required(ErrorMessage = "Default tenant name is required")]
    [MinLength(2, ErrorMessage = "Default tenant name must be at least 2 characters")]
    public string Name { get; set; } = "Default Tenant";

    /// <summary>
    /// Optional domain for the default tenant (e.g., "default.noir.local").
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Optional description for the default tenant.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default tenant admin user configuration.
    /// This user has Admin role within the default tenant only.
    /// </summary>
    public TenantAdminSettings Admin { get; set; } = new();
}

/// <summary>
/// Tenant admin user configuration (for seeding tenant-level admin).
/// </summary>
public class TenantAdminSettings
{
    /// <summary>
    /// Whether to create a default admin for the tenant.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Email address for the tenant admin user.
    /// </summary>
    [Required(ErrorMessage = "Tenant admin email is required")]
    [EmailAddress(ErrorMessage = "Tenant admin email must be a valid email address")]
    public string Email { get; set; } = "admin@noir.local";

    /// <summary>
    /// Password for the tenant admin user.
    /// In production, use environment variables or secrets manager.
    /// </summary>
    [Required(ErrorMessage = "Tenant admin password is required")]
    [MinLength(6, ErrorMessage = "Tenant admin password must be at least 6 characters")]
    public string Password { get; set; } = "123qwe";

    /// <summary>
    /// First name for the tenant admin user.
    /// </summary>
    public string FirstName { get; set; } = "Tenant";

    /// <summary>
    /// Last name for the tenant admin user.
    /// </summary>
    public string LastName { get; set; } = "Administrator";
}
