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
        // With tenant filtering support
        var (roles, totalCount) = await _roleIdentityService.GetRolesPaginatedAsync(
            query.Search,
            query.Page,
            query.PageSize,
            query.TenantId,
            query.IncludeSystemRoles,
            cancellationToken);

        // Get user counts for all roles
        var roleIds = roles.Select(r => r.Id).ToList();
        var userCounts = await _roleIdentityService.GetUserCountsAsync(roleIds, cancellationToken);

        // Map to RoleListDto with all properties
        var roleListDtos = roles.Select(role => new RoleListDto(
            role.Id,
            role.Name,
            role.Description,
            role.ParentRoleId,
            role.IsSystemRole,
            role.SortOrder,
            role.IconName,
            role.Color,
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
