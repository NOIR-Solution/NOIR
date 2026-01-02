namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for assigning permissions to a role.
/// </summary>
public class AssignPermissionToRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public AssignPermissionToRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        AssignPermissionToRoleCommand command,
        CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound("Role", command.RoleId));
        }

        // Get existing permissions
        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        // Add only new permissions
        foreach (var permission in command.Permissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                var claim = new Claim(Permissions.ClaimType, permission);
                await _roleManager.AddClaimAsync(role, claim);
            }
        }

        _cacheInvalidator.InvalidateAll();

        // Return all current permissions
        var allClaims = await _roleManager.GetClaimsAsync(role);
        var allPermissions = allClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        return Result.Success<IReadOnlyList<string>>(allPermissions);
    }
}
