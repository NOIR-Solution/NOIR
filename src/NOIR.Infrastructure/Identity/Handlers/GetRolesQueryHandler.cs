namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting paginated list of roles.
/// </summary>
public class GetRolesQueryHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public GetRolesQueryHandler(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedList<RoleListDto>>> Handle(GetRolesQuery query, CancellationToken ct)
    {
        var rolesQuery = _roleManager.Roles
            .AsNoTracking()
            .TagWith("GetRoles");

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToUpperInvariant();
            rolesQuery = rolesQuery.Where(r => r.NormalizedName!.Contains(search));
        }

        var totalCount = await rolesQuery.CountAsync(ct);

        var roles = await rolesQuery
            .OrderBy(r => r.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Get user counts for each role
        var roleIds = roles.Select(r => r.Id).ToList();
        var userCounts = await _dbContext.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);

        var dtos = roles.Select(r => new RoleListDto(
            r.Id,
            r.Name!,
            userCounts.GetValueOrDefault(r.Id, 0)
        )).ToList();

        return Result.Success(
            PaginatedList<RoleListDto>.Create(dtos, totalCount, query.Page, query.PageSize));
    }
}
