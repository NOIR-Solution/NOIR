namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Handles permission cache invalidation for real-time authorization updates.
/// </summary>
public class PermissionCacheInvalidator : IPermissionCacheInvalidator, IScopedService
{
    private readonly IMemoryCache _cache;
    private readonly UserManager<ApplicationUser> _userManager;
    private const string CacheKeyPrefix = "permissions:";

    // Track all cached user IDs for bulk invalidation
    private static readonly HashSet<string> _cachedUserIds = [];
    private static readonly object _lock = new();

    public PermissionCacheInvalidator(IMemoryCache cache, UserManager<ApplicationUser> userManager)
    {
        _cache = cache;
        _userManager = userManager;
    }

    /// <summary>
    /// Registers a user ID as cached (called by PermissionAuthorizationHandler).
    /// </summary>
    public static void RegisterCachedUser(string userId)
    {
        lock (_lock)
        {
            _cachedUserIds.Add(userId);
        }
    }

    public void InvalidateUser(string userId)
    {
        var cacheKey = $"{CacheKeyPrefix}{userId}";
        _cache.Remove(cacheKey);

        lock (_lock)
        {
            _cachedUserIds.Remove(userId);
        }
    }

    public async Task InvalidateRoleAsync(string roleName)
    {
        // Get all users in this role and invalidate their cache
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

        foreach (var user in usersInRole)
        {
            InvalidateUser(user.Id);
        }
    }

    public void InvalidateAll()
    {
        lock (_lock)
        {
            foreach (var userId in _cachedUserIds.ToList())
            {
                var cacheKey = $"{CacheKeyPrefix}{userId}";
                _cache.Remove(cacheKey);
            }
            _cachedUserIds.Clear();
        }
    }
}
