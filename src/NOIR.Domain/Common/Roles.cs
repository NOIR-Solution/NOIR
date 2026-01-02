namespace NOIR.Domain.Common;

/// <summary>
/// Default role constants for seeding.
/// Roles are database-driven but these constants ensure consistency.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    /// <summary>
    /// Default roles to seed.
    /// </summary>
    public static IReadOnlyList<string> Defaults => [Admin, User];

    /// <summary>
    /// Default role-permission mappings for seeding.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultPermissions =>
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Admin] = Permissions.AdminDefaults,
            [User] = Permissions.UserDefaults
        };
}
