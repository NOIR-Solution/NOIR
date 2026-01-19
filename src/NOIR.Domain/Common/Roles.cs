namespace NOIR.Domain.Common;

/// <summary>
/// Default role constants for seeding.
/// Roles are database-driven but these constants ensure consistency.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Platform-level administrator with full system access.
    /// Can manage all tenants and platform-wide settings.
    /// Users with this role have TenantId = null.
    /// This role is hidden from tenant-level role management (IsPlatformRole = true).
    /// </summary>
    public const string PlatformAdmin = "Platform Admin";

    /// <summary>
    /// Tenant-level administrator with full access within their tenant.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Standard user with basic access within their tenant.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// System roles to seed (platform-level, TenantId = null).
    /// </summary>
    public static IReadOnlyList<string> SystemRoles => [PlatformAdmin];

    /// <summary>
    /// Tenant roles to seed for each new tenant.
    /// </summary>
    public static IReadOnlyList<string> TenantRoles => [Admin, User];

    /// <summary>
    /// All default roles to seed.
    /// </summary>
    public static IReadOnlyList<string> Defaults => [PlatformAdmin, Admin, User];

    /// <summary>
    /// Default role-permission mappings for seeding.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultPermissions =>
        new Dictionary<string, IReadOnlyList<string>>
        {
            [PlatformAdmin] = Permissions.PlatformAdminDefaults,
            [Admin] = Permissions.AdminDefaults,
            [User] = Permissions.UserDefaults
        };
}
