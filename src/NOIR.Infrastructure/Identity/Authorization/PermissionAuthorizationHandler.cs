namespace NOIR.Infrastructure.Identity.Authorization;

/// <summary>
/// Authorization handler that validates permission claims by checking database.
/// Permissions are cached per-user to reduce database queries while still allowing
/// real-time permission updates (cache expires after a short period).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public PermissionAuthorizationHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
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

            // Get effective permissions including inherited from parent roles
            var effectivePermissions = await GetEffectiveRolePermissionsAsync(role);
            permissions.UnionWith(effectivePermissions);
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

    /// <summary>
    /// Gets effective permissions for a role, including inherited permissions from parent roles.
    /// </summary>
    private async Task<HashSet<string>> GetEffectiveRolePermissionsAsync(ApplicationRole role)
    {
        var permissions = new HashSet<string>();
        var visited = new HashSet<string>();

        await CollectPermissionsRecursiveAsync(role, permissions, visited);

        return permissions;
    }

    private async Task CollectPermissionsRecursiveAsync(
        ApplicationRole role,
        HashSet<string> permissions,
        HashSet<string> visited)
    {
        // Prevent infinite loops in case of circular references
        if (!visited.Add(role.Id)) return;

        // Get direct permissions for this role
        var claims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(c => c.Type == Permissions.ClaimType))
        {
            permissions.Add(claim.Value);
        }

        // Recurse to parent role if exists
        if (!string.IsNullOrEmpty(role.ParentRoleId))
        {
            var parentRole = await _roleManager.FindByIdAsync(role.ParentRoleId);
            if (parentRole != null && !parentRole.IsDeleted)
            {
                await CollectPermissionsRecursiveAsync(parentRole, permissions, visited);
            }
        }
    }
}
