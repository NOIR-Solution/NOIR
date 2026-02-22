namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstracts ASP.NET Core Identity operations for role management.
/// This interface allows handlers in the Application layer to perform role operations
/// without directly depending on RoleManager and IdentityRole types.
/// Implementations in Infrastructure layer provide the actual identity logic.
/// </summary>
public interface IRoleIdentityService
{
    #region Role Lookup

    /// <summary>
    /// Finds a role by its unique identifier.
    /// </summary>
    Task<RoleIdentityDto?> FindByIdAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Finds a role by its name.
    /// </summary>
    Task<RoleIdentityDto?> FindByNameAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Checks if a role exists by name.
    /// </summary>
    Task<bool> RoleExistsAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Gets a queryable for paginated role listing.
    /// Note: This returns a projected queryable - use GetRolesPaginatedAsync for EF-compatible pagination.
    /// </summary>
    IQueryable<RoleIdentityDto> GetRolesQueryable();

    /// <summary>
    /// Gets paginated roles with optional search filter.
    /// Handles EF Core translation properly by doing projection after ordering.
    /// </summary>
    Task<(IReadOnlyList<RoleIdentityDto> Roles, int TotalCount)> GetRolesPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets paginated roles with optional search and tenant filter.
    /// When tenantId is specified, returns only system roles (TenantId = null) and tenant-specific roles.
    /// When tenantId is null, returns only system roles.
    /// </summary>
    Task<(IReadOnlyList<RoleIdentityDto> Roles, int TotalCount)> GetRolesPaginatedAsync(
        string? search,
        int page,
        int pageSize,
        Guid? tenantId,
        bool includeSystemRoles,
        CancellationToken ct = default);

    #endregion

    #region Role CRUD

    /// <summary>
    /// Creates a new role with basic name only.
    /// </summary>
    Task<IdentityOperationResult> CreateRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>
    /// Creates a new role with all properties.
    /// </summary>
    Task<IdentityOperationResult> CreateRoleAsync(
        string roleName,
        string? description,
        string? parentRoleId,
        Guid? tenantId,
        bool isSystemRole,
        bool isPlatformRole,
        int sortOrder,
        string? iconName,
        string? color,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a role's name only.
    /// </summary>
    Task<IdentityOperationResult> UpdateRoleAsync(string roleId, string newName, CancellationToken ct = default);

    /// <summary>
    /// Updates a role with all properties.
    /// </summary>
    Task<IdentityOperationResult> UpdateRoleAsync(
        string roleId,
        string newName,
        string? description,
        string? parentRoleId,
        int sortOrder,
        string? iconName,
        string? color,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a role (soft delete).
    /// </summary>
    Task<IdentityOperationResult> DeleteRoleAsync(string roleId, CancellationToken ct = default);

    #endregion

    #region Role Permissions (Claims)

    /// <summary>
    /// Gets all permission claims for a role.
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Adds permissions to a role.
    /// </summary>
    Task<IdentityOperationResult> AddPermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default);

    /// <summary>
    /// Removes permissions from a role.
    /// </summary>
    Task<IdentityOperationResult> RemovePermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default);

    /// <summary>
    /// Sets role permissions, replacing existing ones.
    /// </summary>
    Task<IdentityOperationResult> SetPermissionsAsync(
        string roleId,
        IEnumerable<string> permissions,
        CancellationToken ct = default);

    #endregion

    #region User Count

    /// <summary>
    /// Gets the count of users assigned to a role.
    /// </summary>
    Task<int> GetUserCountAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets user counts for multiple roles in a single query.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetUserCountsAsync(
        IEnumerable<string> roleIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets permission counts for multiple roles in a single query.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetPermissionCountsAsync(
        IEnumerable<string> roleIds,
        CancellationToken ct = default);

    /// <summary>
    /// Gets users assigned to a specific role within a tenant.
    /// </summary>
    /// <param name="roleName">The role name to search for.</param>
    /// <param name="tenantId">The tenant ID to filter users. Pass null for platform-level users.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users with the specified role in the tenant.</returns>
    Task<IReadOnlyList<UserIdentityDto>> GetUsersInRoleAsync(
        string roleName,
        string? tenantId,
        CancellationToken ct = default);

    #endregion

    #region Effective Permissions (with Hierarchy)

    /// <summary>
    /// Gets effective permissions for a role including inherited permissions from parent roles.
    /// </summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets the role hierarchy chain (from child to root).
    /// </summary>
    Task<IReadOnlyList<RoleIdentityDto>> GetRoleHierarchyAsync(string roleId, CancellationToken ct = default);

    #endregion
}

/// <summary>
/// DTO representing role identity information.
/// Decouples Application layer from IdentityRole entity.
/// Includes hierarchy, tenant scoping, and display properties.
/// </summary>
public record RoleIdentityDto(
    string Id,
    string Name,
    string? NormalizedName,
    string? Description = null,
    string? ParentRoleId = null,
    Guid? TenantId = null,
    bool IsSystemRole = false,
    bool IsPlatformRole = false,
    int SortOrder = 0,
    string? IconName = null,
    string? Color = null);
