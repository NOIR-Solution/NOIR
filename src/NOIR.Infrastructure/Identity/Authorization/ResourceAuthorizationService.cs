namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Implementation of resource-based authorization with permission inheritance.
/// </summary>
public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResourceAuthorizationService> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(2);

    public ResourceAuthorizationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<ResourceAuthorizationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> AuthorizeAsync(
        string userId,
        IResource resource,
        string action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        // 1. Check ownership (owner has full access)
        if (resource.OwnerId == userId)
        {
            _logger.LogDebug("User {UserId} authorized as owner of {ResourceType}:{ResourceId}",
                userId, resource.ResourceType, resource.Id);
            return true;
        }

        // 2. Get effective permission (checks direct and inherited)
        var effectivePermission = await GetEffectivePermissionAsync(userId, resource, ct);

        if (effectivePermission.HasValue && effectivePermission.Value.Allows(action))
        {
            _logger.LogDebug(
                "User {UserId} authorized for {Action} on {ResourceType}:{ResourceId} with permission {Permission}",
                userId, action, resource.ResourceType, resource.Id, effectivePermission.Value);
            return true;
        }

        _logger.LogDebug(
            "User {UserId} denied {Action} on {ResourceType}:{ResourceId}",
            userId, action, resource.ResourceType, resource.Id);
        return false;
    }

    public async Task<bool> AuthorizeAsync(
        string userId,
        string resourceType,
        Guid resourceId,
        string action,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        // Check direct share first (can't check ownership without loading resource)
        var requiredPermission = SharePermissionExtensions.FromAction(action);
        if (!requiredPermission.HasValue)
        {
            return false;
        }

        var normalizedType = resourceType.ToLowerInvariant();
        var cacheKey = $"resource_auth:{normalizedType}:{resourceId}:{userId}";

        if (_cache.TryGetValue(cacheKey, out SharePermission? cachedPermission))
        {
            return cachedPermission.HasValue && cachedPermission.Value.Allows(action);
        }

        var share = await SpecificationEvaluator
            .GetQuery(_context.ResourceShares, new ResourceShareByUserSpec(resourceType, resourceId, userId))
            .FirstOrDefaultAsync(ct);

        var permission = share?.Permission;

        _cache.Set(cacheKey, permission, new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheExpiration)
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));

        return permission.HasValue && permission.Value.Allows(action);
    }

    public async Task<SharePermission?> GetEffectivePermissionAsync(
        string userId,
        IResource resource,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Owner has Admin permission
        if (resource.OwnerId == userId)
        {
            return SharePermission.Admin;
        }

        // Check cache first
        var cacheKey = $"resource_perm:{resource.ResourceType}:{resource.Id}:{userId}";
        if (_cache.TryGetValue(cacheKey, out SharePermission? cached))
        {
            return cached;
        }

        // 1. Check direct share on this resource
        var directShare = await SpecificationEvaluator
            .GetQuery(_context.ResourceShares, new ResourceShareByUserSpec(resource.ResourceType, resource.Id, userId))
            .FirstOrDefaultAsync(ct);

        if (directShare != null)
        {
            CachePermission(cacheKey, directShare.Permission);
            return directShare.Permission;
        }

        // 2. Check inherited permission from parent hierarchy
        if (resource.ParentResourceId.HasValue && !string.IsNullOrEmpty(resource.ParentResourceType))
        {
            var inheritedPermission = await GetInheritedPermissionAsync(
                resource.ParentResourceType,
                resource.ParentResourceId.Value,
                userId,
                ct);

            if (inheritedPermission.HasValue)
            {
                CachePermission(cacheKey, inheritedPermission.Value);
                return inheritedPermission.Value;
            }
        }

        // No permission found
        CachePermission(cacheKey, null);
        return null;
    }

    public async Task<IReadOnlyList<(Guid ResourceId, SharePermission Permission)>> GetAccessibleResourcesAsync(
        string userId,
        string resourceType,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        var shares = await SpecificationEvaluator
            .GetQuery(_context.ResourceShares, new ResourceSharesByUserSpec(userId, resourceType))
            .ToListAsync(ct);

        return shares
            .Select(s => (s.ResourceId, s.Permission))
            .ToList();
    }

    /// <summary>
    /// Walks up the parent hierarchy to find inherited permission.
    /// </summary>
    private async Task<SharePermission?> GetInheritedPermissionAsync(
        string parentType,
        Guid parentId,
        string userId,
        CancellationToken ct,
        int depth = 0)
    {
        // Prevent infinite loops (max 10 levels deep)
        if (depth > 10)
        {
            _logger.LogWarning("Permission inheritance depth exceeded for user {UserId}", userId);
            return null;
        }

        var parentShare = await SpecificationEvaluator
            .GetQuery(_context.ResourceShares, new ResourceShareByUserSpec(parentType, parentId, userId))
            .FirstOrDefaultAsync(ct);

        if (parentShare != null)
        {
            return parentShare.Permission;
        }

        // TODO: If parent resource also has a parent, continue walking up
        // This would require loading the parent resource to get its ParentResourceId
        // For now, we only support single-level inheritance
        // Future: Inject IResourceLoader to load parent and continue

        return null;
    }

    private void CachePermission(string cacheKey, SharePermission? permission)
    {
        _cache.Set(cacheKey, permission, new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheExpiration)
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
    }
}
