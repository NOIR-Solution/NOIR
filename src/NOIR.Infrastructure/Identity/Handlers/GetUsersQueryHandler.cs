namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting paginated list of users.
/// </summary>
public class GetUsersQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public GetUsersQueryHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedList<UserListDto>>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var usersQuery = _userManager.Users
            .AsNoTracking()
            .TagWith("GetUsers");

        // Apply search filter using EF.Functions.Like for better index usage
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search}%";
            usersQuery = usersQuery.Where(u =>
                EF.Functions.Like(u.Email!, searchPattern) ||
                (u.DisplayName != null && EF.Functions.Like(u.DisplayName, searchPattern)) ||
                (u.FirstName != null && EF.Functions.Like(u.FirstName, searchPattern)) ||
                (u.LastName != null && EF.Functions.Like(u.LastName, searchPattern)));
        }

        // Apply lockout filter
        if (query.IsLocked.HasValue)
        {
            var lockoutNow = DateTimeOffset.UtcNow;
            if (query.IsLocked.Value)
            {
                usersQuery = usersQuery.Where(u => u.LockoutEnd != null && u.LockoutEnd > lockoutNow);
            }
            else
            {
                usersQuery = usersQuery.Where(u => u.LockoutEnd == null || u.LockoutEnd <= lockoutNow);
            }
        }

        // Apply role filter if specified
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var roleId = await _dbContext.Roles
                .Where(r => r.NormalizedName == query.Role.ToUpperInvariant())
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (roleId is not null)
            {
                var userIdsInRole = _dbContext.UserRoles
                    .Where(ur => ur.RoleId == roleId)
                    .Select(ur => ur.UserId);
                usersQuery = usersQuery.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        var totalCount = await usersQuery.CountAsync(ct);

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // Get roles for each user
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToListAsync(ct);

        var rolesByUser = userRoles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName!).ToList());

        var now = DateTimeOffset.UtcNow;
        var dtos = users.Select(u => new UserListDto(
            u.Id,
            u.Email!,
            u.DisplayName,
            u.LockoutEnd.HasValue && u.LockoutEnd > now,
            rolesByUser.GetValueOrDefault(u.Id, [])
        )).ToList();

        return Result.Success(
            PaginatedList<UserListDto>.Create(dtos, totalCount, query.Page, query.PageSize));
    }
}
