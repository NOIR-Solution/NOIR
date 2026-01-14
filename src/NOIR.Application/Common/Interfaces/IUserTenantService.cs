namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing user-tenant memberships.
/// Users can belong to multiple tenants with different roles.
/// </summary>
public interface IUserTenantService
{
    /// <summary>
    /// Gets all tenants that a user belongs to.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tenant memberships with tenant info.</returns>
    Task<IReadOnlyList<UserTenantMembershipDto>> GetUserTenantsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user memberships with user info.</returns>
    Task<IReadOnlyList<TenantUserMembershipDto>> GetTenantUsersAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated users in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="searchTerm">Optional search term for user name/email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of user memberships.</returns>
    Task<PaginatedTenantUsersDto> GetTenantUsersPaginatedAsync(
        string tenantId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a tenant with a specific role.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="role">The role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created membership, or failure if user is already in tenant.</returns>
    Task<Result<UserTenantMembershipDto>> AddUserToTenantAsync(
        string userId,
        string tenantId,
        TenantRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from a tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if removed, false if membership didn't exist.</returns>
    Task<Result<bool>> RemoveUserFromTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's role in a tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="newRole">The new role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated membership, or failure if user is not in tenant.</returns>
    Task<Result<UserTenantMembershipDto>> UpdateUserRoleInTenantAsync(
        string userId,
        string tenantId,
        TenantRole newRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's role in a specific tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role, or null if user is not in tenant.</returns>
    Task<TenantRole?> GetUserRoleInTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user belongs to a tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user is a member of the tenant.</returns>
    Task<bool> IsUserInTenantAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's membership in a specific tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The membership details, or null if not found.</returns>
    Task<UserTenantMembershipDto?> GetMembershipAsync(
        string userId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of users in the tenant.</returns>
    Task<int> GetTenantUserCountAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO representing a user's membership in a tenant.
/// </summary>
public record UserTenantMembershipDto(
    string TenantId,
    string TenantName,
    string TenantSlug,
    TenantRole Role,
    DateTimeOffset JoinedAt,
    bool IsDefault);

/// <summary>
/// DTO representing a tenant user with basic info.
/// </summary>
public record TenantUserMembershipDto(
    string UserId,
    string Email,
    string? FullName,
    string? AvatarUrl,
    TenantRole Role,
    DateTimeOffset JoinedAt,
    bool IsActive);

/// <summary>
/// Paginated result of tenant users.
/// </summary>
public record PaginatedTenantUsersDto(
    IReadOnlyList<TenantUserMembershipDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
