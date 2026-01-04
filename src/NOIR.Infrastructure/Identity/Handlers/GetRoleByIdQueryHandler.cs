namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting a role by ID with its permissions.
/// </summary>
public class GetRoleByIdQueryHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILocalizationService _localization;

    public GetRoleByIdQueryHandler(
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext,
        ILocalizationService localization)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(GetRoleByIdQuery query, CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(query.RoleId);
        if (role is null)
        {
            return Result.Failure<RoleDto>(
                Error.NotFound(_localization["auth.role.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        var userCount = await _dbContext.UserRoles
            .TagWith("GetRoleById_UserCount")
            .CountAsync(ur => ur.RoleId == role.Id, ct);

        return Result.Success(new RoleDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            userCount,
            permissions));
    }
}
