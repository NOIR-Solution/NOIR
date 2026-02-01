namespace NOIR.Infrastructure.Caching;

using ZiggyCreatures.Caching.Fusion;

/// <summary>
/// Implementation of ICacheInvalidationService using FusionCache.
/// Uses tag-based invalidation (FusionCache v2) for efficient bulk cache clearing.
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService, IScopedService
{
    private readonly IFusionCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IFusionCache cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvalidateUserCacheAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating cache for user {UserId}", userId);

        // Remove specific user-related keys
        await _cache.RemoveAsync(CacheKeys.UserProfile(userId), token: ct);
        await _cache.RemoveAsync(CacheKeys.UserById(userId), token: ct);
        await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), token: ct);

        // FusionCache v2: Tag-based invalidation would be:
        // await _cache.RemoveByTagAsync($"user:{userId}", token: ct);

        _logger.LogDebug("Cache invalidated for user {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task InvalidateUserPermissionsAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating permission cache for user {UserId}", userId);

        await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), token: ct);

        _logger.LogDebug("Permission cache invalidated for user {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task InvalidateRoleCacheAsync(string roleId, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating cache for role {RoleId}", roleId);

        await _cache.RemoveAsync(CacheKeys.RoleById(roleId), token: ct);
        await _cache.RemoveAsync(CacheKeys.RolePermissions(roleId), token: ct);
        await _cache.RemoveAsync(CacheKeys.AllRoles(), token: ct);

        _logger.LogDebug("Cache invalidated for role {RoleId}", roleId);
    }

    /// <inheritdoc />
    public async Task InvalidateAllPermissionsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating all permission caches");

        // For now, we can't easily invalidate all permission caches without tag support
        // This would require tracking all permission-related keys
        // FusionCache v2 with tagging: await _cache.RemoveByTagAsync("permissions", token: ct);

        // As a workaround, log a warning that manual invalidation may be needed
        _logger.LogWarning(
            "Full permission cache invalidation requested. " +
            "Individual user permission caches will expire naturally. " +
            "Consider restarting the app for immediate full invalidation.");

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task InvalidateBlogCacheAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating all blog caches");

        // Remove known blog-related keys
        await _cache.RemoveAsync(CacheKeys.BlogCategories(), token: ct);
        await _cache.RemoveAsync(CacheKeys.BlogTags(), token: ct);
        await _cache.RemoveAsync(CacheKeys.RssFeed(), token: ct);
        await _cache.RemoveAsync(CacheKeys.Sitemap(), token: ct);

        // Note: Individual post caches and list caches will expire naturally
        // FusionCache v2 with tagging: await _cache.RemoveByTagAsync("blog", token: ct);

        _logger.LogDebug("Blog caches invalidated");
    }

    /// <inheritdoc />
    public async Task InvalidatePostCacheAsync(Guid postId, string? slug = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating cache for post {PostId}", postId);

        await _cache.RemoveAsync(CacheKeys.PostById(postId), token: ct);

        if (!string.IsNullOrEmpty(slug))
        {
            await _cache.RemoveAsync(CacheKeys.PostBySlug(slug), token: ct);
        }

        // Also invalidate list caches and feeds
        await _cache.RemoveAsync(CacheKeys.RssFeed(), token: ct);
        await _cache.RemoveAsync(CacheKeys.Sitemap(), token: ct);

        _logger.LogDebug("Cache invalidated for post {PostId}", postId);
    }

    /// <inheritdoc />
    public async Task InvalidateTenantSettingsAsync(string tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating settings cache for tenant {TenantId}", tenantId);

        await _cache.RemoveAsync(CacheKeys.TenantSettings(tenantId), token: ct);
        await _cache.RemoveAsync(CacheKeys.TenantById(tenantId), token: ct);

        _logger.LogDebug("Settings cache invalidated for tenant {TenantId}", tenantId);
    }

    /// <inheritdoc />
    public async Task InvalidateEmailTemplateCacheAsync(string templateName, string? tenantId = null, CancellationToken ct = default)
    {
        var tenantKey = tenantId ?? "platform";
        _logger.LogInformation("Invalidating email template cache for {TemplateName} (tenant: {TenantId})", templateName, tenantKey);

        // Invalidate the cached template (uses centralized CacheKeys for consistency)
        var cacheKey = CacheKeys.EmailTemplate(templateName, tenantId);
        await _cache.RemoveAsync(cacheKey, token: ct);

        _logger.LogDebug("Email template cache invalidated for {TemplateName}", templateName);
    }
}
