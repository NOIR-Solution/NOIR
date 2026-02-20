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
    /// The user's display name (friendly name for UI display).
    /// Falls back to Email if not set.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// The current active tenant ID (from JWT claim).
    /// Null if user is not in a tenant context.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The user's roles (e.g., Admin, User).
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="role">The role name to check.</param>
    /// <returns>True if the user has the role.</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Checks if the user is a platform administrator.
    /// Platform admins have elevated access across all tenants.
    /// </summary>
    bool IsPlatformAdmin { get; }
}
