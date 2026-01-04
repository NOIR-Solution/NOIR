namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for removing permissions from a role.
/// </summary>
public class RemovePermissionFromRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;
    private readonly ILocalizationService _localization;

    public RemovePermissionFromRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator,
        ILocalizationService localization)
    {
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
        _localization = localization;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        RemovePermissionFromRoleCommand command,
        CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role is null)
        {
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound(_localization["auth.role.notFound"], ErrorCodes.Auth.RoleNotFound));
        }

        // Remove specified permissions
        foreach (var permission in command.Permissions)
        {
            var claim = new Claim(Permissions.ClaimType, permission);
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        _cacheInvalidator.InvalidateAll();

        // Return remaining permissions
        var remainingClaims = await _roleManager.GetClaimsAsync(role);
        var remainingPermissions = remainingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        return Result.Success<IReadOnlyList<string>>(remainingPermissions);
    }
}
