namespace NOIR.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Wolverine handler for getting a paginated list of users.
/// Supports search, role filtering, and lockout status filtering.
/// </summary>
public class GetUsersQueryHandler
{
    private readonly IUserIdentityService _userIdentityService;

    public GetUsersQueryHandler(IUserIdentityService userIdentityService)
    {
        _userIdentityService = userIdentityService;
    }

    public async Task<Result<PaginatedList<UserListDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        // Use the paginated method which handles EF Core translation properly
        var (users, totalCount) = await _userIdentityService.GetUsersPaginatedAsync(
            query.Search,
            query.Page,
            query.PageSize,
            cancellationToken);

        // Map to UserListDto with roles
        var userListDtos = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userIdentityService.GetRolesAsync(user.Id, cancellationToken);
            var isLocked = !user.IsActive;

            // Apply role filter if specified
            if (!string.IsNullOrWhiteSpace(query.Role) && !roles.Contains(query.Role, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Apply lockout filter if specified
            if (query.IsLocked.HasValue && query.IsLocked.Value != isLocked)
            {
                continue;
            }

            userListDtos.Add(new UserListDto(
                user.Id,
                user.Email,
                user.DisplayName ?? user.FullName,
                isLocked,
                user.IsSystemUser,
                roles));
        }

        var result = PaginatedList<UserListDto>.Create(
            userListDtos,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
