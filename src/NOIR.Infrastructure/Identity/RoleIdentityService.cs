using NOIR.Domain.Common;

namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Implementation of IRoleIdentityService that wraps ASP.NET Core Identity.
/// Provides role management operations for handlers in the Application layer.
/// </summary>
public class RoleIdentityService : IRoleIdentityService, IScopedService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public RoleIdentityService(
        RoleManager<IdentityRole> roleManager,
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
            .Select(r => new RoleIdentityDto(
                r.Id,
                r.Name!,
                r.NormalizedName));
    }

    public async Task<(IReadOnlyList<RoleIdentityDto> Roles, int TotalCount)> GetRolesPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _roleManager.Roles.AsQueryable();

        // Apply search filter on raw entity (before projection)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(r => r.Name != null && r.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);

        // Order, paginate, then project
        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleIdentityDto(
                r.Id,
                r.Name!,
                r.NormalizedName))
            .ToListAsync(ct);

        return (roles, totalCount);
    }

    #endregion

    #region Role CRUD

    public async Task<IdentityOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default)
    {
        var role = new IdentityRole(roleName);
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
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return IdentityOperationResult.Failure("Role not found.");
        }

        role.Name = newName;
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

        var result = await _roleManager.DeleteAsync(role);

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
            .CountAsync(ur => ur.RoleId == roleId, ct);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetUserCountsAsync(
        IEnumerable<string> roleIds,
        CancellationToken ct = default)
    {
        var roleIdList = roleIds.ToList();
        return await _dbContext.UserRoles
            .TagWith("RoleIdentityService_GetUserCounts")
            .Where(ur => roleIdList.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
    }

    #endregion

    #region Mapping

    private static RoleIdentityDto MapToDto(IdentityRole role)
    {
        return new RoleIdentityDto(
            role.Id,
            role.Name!,
            role.NormalizedName);
    }

    #endregion
}
