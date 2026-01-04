namespace NOIR.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Wolverine handler for getting a paginated list of roles.
/// Supports search filtering.
/// </summary>
public class GetRolesQueryHandler
{
    private readonly IRoleIdentityService _roleIdentityService;

    public GetRolesQueryHandler(IRoleIdentityService roleIdentityService)
    {
        _roleIdentityService = roleIdentityService;
    }

    public async Task<Result<PaginatedList<RoleListDto>>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        // Use the paginated method which handles EF Core translation properly
        var (roles, totalCount) = await _roleIdentityService.GetRolesPaginatedAsync(
            query.Search,
            query.Page,
            query.PageSize,
            cancellationToken);

        // Get user counts for all roles
        var roleIds = roles.Select(r => r.Id).ToList();
        var userCounts = await _roleIdentityService.GetUserCountsAsync(roleIds, cancellationToken);

        // Map to RoleListDto
        var roleListDtos = roles.Select(role => new RoleListDto(
            role.Id,
            role.Name,
            userCounts.TryGetValue(role.Id, out var count) ? count : 0
        )).ToList();

        var result = PaginatedList<RoleListDto>.Create(
            roleListDtos,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
