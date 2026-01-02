namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization handler that validates permission claims by checking database.
/// Permissions are cached per-user to reduce database queries while still allowing
/// real-time permission updates (cache expires after a short period).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public PermissionAuthorizationHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var permissions = await GetUserPermissionsAsync(userId);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<HashSet<string>> GetUserPermissionsAsync(string userId)
    {
        var cacheKey = $"permissions:{userId}";

        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPermissions) && cachedPermissions is not null)
        {
            return cachedPermissions;
        }

        var permissions = new HashSet<string>();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return permissions;
        }

        var roles = await _userManager.GetRolesAsync(user);

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(c => c.Type == Permissions.ClaimType))
            {
                permissions.Add(claim.Value);
            }
        }

        // Cache permissions with sliding expiration
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheExpiration)
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(cacheKey, permissions, cacheOptions);

        // Register for bulk invalidation tracking
        PermissionCacheInvalidator.RegisterCachedUser(userId);

        return permissions;
    }
}
