namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for invalidating permission cache when roles/permissions change.
/// Call this when:
/// - User is added/removed from a role
/// - Role permissions are modified
/// </summary>
public interface IPermissionCacheInvalidator
{
    /// <summary>
    /// Invalidates cached permissions for a specific user.
    /// Call when user's roles change.
    /// </summary>
    void InvalidateUser(string userId);

    /// <summary>
    /// Invalidates cached permissions for all users in a role.
    /// Call when role's permissions change.
    /// </summary>
    Task InvalidateRoleAsync(string roleName);

    /// <summary>
    /// Invalidates all cached permissions.
    /// Use sparingly - only for bulk operations.
    /// </summary>
    void InvalidateAll();
}
