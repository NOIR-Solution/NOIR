namespace NOIR.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Wolverine handler for getting a paginated list of users.
/// Supports search, role filtering, and lockout status filtering.
/// Users are filtered by the current tenant context.
/// </summary>
public class GetUsersQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;

    public GetUsersQueryHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedList<UserListDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        // Use the paginated method which handles EF Core translation properly
        // Users are scoped to the current tenant context
        // Role and lockout filters are applied at the database level for accurate pagination
        var (users, totalCount) = await _userIdentityService.GetUsersPaginatedAsync(
            _currentUser.TenantId,
            query.Search,
            query.Page,
            query.PageSize,
            query.Role,
            query.IsLocked,
            cancellationToken);

        // Batch fetch roles for all users in a single operation (fixes N+1)
        var userIds = users.Select(u => u.Id).ToList();
        var rolesDict = await _userIdentityService.GetRolesForUsersAsync(userIds, cancellationToken);

        // Map to UserListDto with roles (filtering already done at database level)
        var userListDtos = users.Select(user =>
        {
            var roles = rolesDict.TryGetValue(user.Id, out var userRoles) ? userRoles : [];
            var isLocked = !user.IsActive;

            return new UserListDto(
                user.Id,
                user.Email,
                user.DisplayName ?? user.FullName,
                isLocked,
                user.IsSystemUser,
                roles);
        }).ToList();

        var result = PaginatedList<UserListDto>.Create(
            userListDtos,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
