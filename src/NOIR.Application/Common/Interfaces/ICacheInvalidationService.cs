namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for cache invalidation operations.
/// Provides methods to invalidate cache entries by key, tag, or category.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidate all cache entries for a specific user.
    /// </summary>
    Task InvalidateUserCacheAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all permission-related cache entries for a user.
    /// </summary>
    Task InvalidateUserPermissionsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cache entries for a specific role.
    /// </summary>
    Task InvalidateRoleCacheAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all permission-related cache entries (e.g., when permissions are modified).
    /// </summary>
    Task InvalidateAllPermissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate all blog-related cache entries.
    /// </summary>
    Task InvalidateBlogCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache for a specific blog post.
    /// </summary>
    Task InvalidatePostCacheAsync(Guid postId, string? slug = null, CancellationToken ct = default);

    /// <summary>
    /// Invalidate tenant settings cache.
    /// </summary>
    Task InvalidateTenantSettingsAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate email template cache for a specific template and tenant.
    /// </summary>
    Task InvalidateEmailTemplateCacheAsync(string templateName, string? tenantId = null, CancellationToken ct = default);
}
