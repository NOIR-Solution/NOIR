using NOIR.Domain.Common;

namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Implementation of IRoleIdentityService that wraps ASP.NET Core Identity.
/// Provides role management operations for handlers in the Application layer.
/// Supports role hierarchy with permission inheritance.
/// </summary>
public class RoleIdentityService : IRoleIdentityService, IScopedService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public RoleIdentityService(
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    #region Role Lookup

    public async Task<RoleIdentityDto?> FindByIdAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        return role is null ? null : MapToDto(role);
    }

    public async Task<RoleIdentityDto?> FindByNameAsync(string roleName, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        return role is null ? null : MapToDto(role);
    }

    public async Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }

    public IQueryable<RoleIdentityDto> GetRolesQueryable()
    {
        return _roleManager.Roles
            .Where(r => !r.IsDeleted)
            .Select(r => new RoleIdentityDto(
                r.Id,
                r.Name!,
                r.NormalizedName,
                r.Description,
                r.ParentRoleId,
                r.TenantId,
                r.IsSystemRole,
                r.IsPlatformRole,
                r.SortOrder,
                r.IconName,
                r.Color));
    }

    public async Task<(IReadOnlyList<RoleIdentityDto> Roles, int TotalCount)> GetRolesPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _roleManager.Roles.Where(r => !r.IsDeleted).AsQueryable();

        // Apply search filter on raw entity (before projection)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        // Order, paginate, then project
        var roles = await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleIdentityDto(
                r.Id,
                r.Name!,
                r.NormalizedName,
                r.Description,
                r.ParentRoleId,
                r.TenantId,
                r.IsSystemRole,
                r.IsPlatformRole,
                r.SortOrder,
                r.IconName,
                r.Color))
            .ToListAsync(ct);

        return (roles, totalCount);
    }

    public async Task<(IReadOnlyList<RoleIdentityDto> Roles, int TotalCount)> GetRolesPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        Guid? tenantId,
        bool includeSystemRoles,
        CancellationToken ct = default)
    {
        var query = _roleManager.Roles.Where(r => !r.IsDeleted).AsQueryable();

        // Always exclude platform roles from tenant-level UI
        // Platform roles are for cross-tenant administration and should be hidden
        query = query.Where(r => !r.IsPlatformRole);

        // Apply tenant filtering:
        // - If tenantId is specified: include tenant-specific roles AND optionally system roles
        // - If tenantId is null: include only system roles (global roles)
        if (tenantId.HasValue)
        {
            if (includeSystemRoles)
            {
                // Include system roles (TenantId = null) AND tenant-specific roles
                query = query.Where(r => r.TenantId == null || r.TenantId == tenantId.Value);
            }
            else
            {
                // Only tenant-specific roles
                query = query.Where(r => r.TenantId == tenantId.Value);
            }
        }
        else
        {
            // Only system roles (no tenant context)
            query = query.Where(r => r.TenantId == null);
        }

        // Apply search filter on raw entity (before projection)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        // Order, paginate, then project
        var roles = await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleIdentityDto(
                r.Id,
                r.Name!,
                r.NormalizedName,
                r.Description,
                r.ParentRoleId,
                r.TenantId,
                r.IsSystemRole,
                r.IsPlatformRole,
                r.SortOrder,
                r.IconName,
                r.Color))
            .ToListAsync(ct);

        return (roles, totalCount);
    }

    #endregion

    #region Role CRUD

    public async Task<IdentityOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default)
    {
        return await CreateRoleAsync(roleName, null, null, null, false, false, 0, null, null, ct);
    }

    public async Task<IdentityOperationResult> CreateRoleAsync(
        string roleName,
        string? description,
        string? parentRoleId,
        Guid? tenantId,
        bool isSystemRole,
        bool isPlatformRole,
        int sortOrder,
        string? iconName,
        string? color,
        CancellationToken ct = default)
    {
        var role = ApplicationRole.Create(
            roleName,
            description,
            parentRoleId,
            tenantId,
            isSystemRole,
            isPlatformRole,
            sortOrder,
            iconName,
            color);

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(role.Id);
    }

    public async Task<IdentityOperationResult> UpdateRoleAsync(
        string roleId,
        string newName,
        CancellationToken ct = default)
    {
        return await UpdateRoleAsync(roleId, newName, null, null, 0, null, null, ct);
    }

    public async Task<IdentityOperationResult> UpdateRoleAsync(
        string roleId,
        string newName,
        string? description,
        string? parentRoleId,
        int sortOrder,
        string? iconName,
        string? color,
        CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        // Platform roles cannot be modified at all
        if (role.IsPlatformRole)
        {
            return IdentityOperationResult.Failure("Cannot modify a platform role.");
        }

        if (role.IsSystemRole && role.Name != newName)
        {
            return IdentityOperationResult.Failure("Cannot rename a system role.");
        }

        role.Update(newName, description, parentRoleId, sortOrder, iconName, color);
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(role.Id);
    }

    public async Task<IdentityOperationResult> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        // Platform roles cannot be deleted
        if (role.IsPlatformRole)
        {
            return IdentityOperationResult.Failure("Cannot delete a platform role.");
        }

        if (role.IsSystemRole)
        {
            return IdentityOperationResult.Failure("Cannot delete a system role.");
        }

        // Check for child roles
        var hasChildRoles = await _roleManager.Roles
            .AnyAsync(r => r.ParentRoleId == roleId && !r.IsDeleted, ct);
        if (hasChildRoles)
        {
            return IdentityOperationResult.Failure("Cannot delete a role that has child roles.");
        }

        // Soft delete
        role.IsDeleted = true;
        role.DeletedAt = DateTimeOffset.UtcNow;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(
                result.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(role.Id);
    }

    #endregion

    #region Role Permissions (Claims)

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return [];
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();
    }

    public async Task<IdentityOperationResult> AddPermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in permissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                var claim = new Claim(Permissions.ClaimType, permission);
                await _roleManager.AddClaimAsync(role, claim);
            }
        }

        return IdentityOperationResult.Success(role.Id);
    }

    public async Task<IdentityOperationResult> RemovePermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        foreach (var permission in permissions)
        {
            var claim = new Claim(Permissions.ClaimType, permission);
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        return IdentityOperationResult.Success(role.Id);
    }

    public async Task<IdentityOperationResult> SetPermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        var newPermissions = permissions.ToHashSet();

        // Get existing permissions
        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        // Remove permissions that are no longer needed
        var toRemove = existingPermissions.Except(newPermissions);
        foreach (var permission in toRemove)
        {
            var claim = new Claim(Permissions.ClaimType, permission);
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        // Add new permissions
        var toAdd = newPermissions.Except(existingPermissions);
        foreach (var permission in toAdd)
        {
            var claim = new Claim(Permissions.ClaimType, permission);
            await _roleManager.AddClaimAsync(role, claim);
        }

        return IdentityOperationResult.Success(role.Id);
    }

    #endregion

    #region User Count

    public async Task<int> GetUserCountAsync(string roleId, CancellationToken ct = default)
    {
        return await _dbContext.UserRoles
            .TagWith("RoleIdentityService_GetUserCount")
            .Join(_dbContext.Users.Where(u => !u.IsDeleted),
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => ur)
            .CountAsync(ur => ur.RoleId == roleId, ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetUserCountsAsync(
        IEnumerable<string> roleIds,
        CancellationToken ct = default)
    {
        var roleIdList = roleIds.ToList();
        return await _dbContext.UserRoles
            .TagWith("RoleIdentityService_GetUserCounts")
            .Join(_dbContext.Users.Where(u => !u.IsDeleted),
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => ur)
            .Where(ur => roleIdList.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
    }

    public async Task<IReadOnlyList<UserIdentityDto>> GetUsersInRoleAsync(
        string roleName,
        string? tenantId,
        CancellationToken ct = default)
    {
        // Find the role by name
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return [];
        }

        // Query users in this role filtered by tenant
        var users = await _dbContext.UserRoles
            .TagWith("RoleIdentityService_GetUsersInRole")
            .Where(ur => ur.RoleId == role.Id)
            .Join(
                _dbContext.Users.Where(u => !u.IsDeleted && u.IsActive && u.TenantId == tenantId),
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => u)
            .Select(u => new UserIdentityDto(
                u.Id,
                u.Email ?? string.Empty,
                u.TenantId,
                u.FirstName,
                u.LastName,
                u.DisplayName,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.PhoneNumber,
                u.AvatarUrl,
                u.IsActive,
                u.IsDeleted,
                u.IsSystemUser,
                u.CreatedAt,
                u.ModifiedAt))
            .ToListAsync(ct);

        return users;
    }

    #endregion

    #region Effective Permissions (with Hierarchy)

    /// <summary>
    /// Gets effective permissions for a role including inherited permissions from parent roles.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default)
    {
        var permissions = new HashSet<string>();
        var visited = new HashSet<string>();

        await CollectPermissionsRecursiveAsync(roleId, permissions, visited, ct);

        return permissions.ToList();
    }

    private async Task CollectPermissionsRecursiveAsync(
        string roleId,
        HashSet<string> permissions,
        HashSet<string> visited,
        CancellationToken ct)
    {
        // Prevent infinite loops in case of circular references
        if (!visited.Add(roleId)) return;

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null || role.IsDeleted) return;

        // Get direct permissions
        var claims = await _roleManager.GetClaimsAsync(role);
        var directPermissions = claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value);
        permissions.UnionWith(directPermissions);

        // Recurse to parent
        if (!string.IsNullOrEmpty(role.ParentRoleId))
        {
            await CollectPermissionsRecursiveAsync(role.ParentRoleId, permissions, visited, ct);
        }
    }

    /// <summary>
    /// Gets the role hierarchy chain (from child to root).
    /// </summary>
    public async Task<IReadOnlyList<RoleIdentityDto>> GetRoleHierarchyAsync(string roleId, CancellationToken ct = default)
    {
        var hierarchy = new List<RoleIdentityDto>();
        var visited = new HashSet<string>();
        var currentRoleId = roleId;

        while (!string.IsNullOrEmpty(currentRoleId) && visited.Add(currentRoleId))
        {
            var role = await _roleManager.FindByIdAsync(currentRoleId);
            if (role == null || role.IsDeleted) break;

            hierarchy.Add(MapToDto(role));
            currentRoleId = role.ParentRoleId;
        }

        return hierarchy;
    }

    #endregion

    #region Mapping

    private static RoleIdentityDto MapToDto(ApplicationRole role)
    {
        return new RoleIdentityDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            role.Description,
            role.ParentRoleId,
            role.TenantId,
            role.IsSystemRole,
            role.IsPlatformRole,
            role.SortOrder,
            role.IconName,
            role.Color);
    }

    #endregion
}
