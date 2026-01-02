namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for getting all permissions assigned to a role.
/// </summary>
public class GetRolePermissionsQueryHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public GetRolePermissionsQueryHandler(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetRolePermissionsQuery query,
        CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(query.RoleId);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound("Role", query.RoleId));
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        return Result.Success<IReadOnlyList<string>>(permissions);
    }
}
