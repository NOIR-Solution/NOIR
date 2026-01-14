namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for accessing the current authenticated user and tenant context.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The current active tenant ID (from JWT claim or session).
    /// Null if user is not in a tenant context.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The user's role in the current tenant.
    /// Null if TenantId is null or user has no role in the tenant.
    /// </summary>
    TenantRole? TenantRole { get; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The user's platform-level roles (e.g., Admin, User).
    /// These are distinct from tenant roles.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Checks if the user has a specific platform-level role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user has the role.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the user has at least the specified tenant role level in the current tenant.
    /// </summary>
    /// <param name="minimumRole">The minimum required role.</param>
    /// <returns>True if the user has at least the specified role level.</returns>
    bool HasTenantRole(TenantRole minimumRole);

    /// <summary>
    /// Checks if the user is a platform administrator.
    /// Platform admins have elevated access across all tenants.
    /// </summary>
    bool IsPlatformAdmin { get; }
}
