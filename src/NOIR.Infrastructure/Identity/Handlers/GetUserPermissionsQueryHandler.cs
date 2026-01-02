namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting effective permissions for a user (combined from all roles).
/// </summary>
public class GetUserPermissionsQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public GetUserPermissionsQueryHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<UserPermissionsDto>> Handle(
        GetUserPermissionsQuery query,
        CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(query.UserId);
        if (user is null)
        {
            return Result.Failure<UserPermissionsDto>(Error.NotFound("User", query.UserId));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var allPermissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is not null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == Permissions.ClaimType))
                {
                    allPermissions.Add(claim.Value);
                }
            }
        }

        return Result.Success(new UserPermissionsDto(
            user.Id,
            user.Email!,
            roles.ToList(),
            allPermissions.OrderBy(p => p).ToList()));
    }
}
