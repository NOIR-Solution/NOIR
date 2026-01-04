namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for creating a new role with optional permissions.
/// </summary>
public class CreateRoleCommandHandler
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;
    private readonly ILocalizationService _localization;

    public CreateRoleCommandHandler(
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator,
        ILocalizationService localization)
    {
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
        _localization = localization;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand command, CancellationToken ct)
    {
        // Check if role already exists
        if (await _roleManager.RoleExistsAsync(command.Name))
        {
            return Result.Failure<RoleDto>(
                Error.Conflict(_localization["auth.role.alreadyExists"], ErrorCodes.Business.AlreadyExists));
        }

        var role = new IdentityRole(command.Name);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return Result.Failure<RoleDto>(Error.Failure(ErrorCodes.System.DatabaseError, _localization["auth.role.createFailed"]));
        }

        // Assign permissions if provided
        var permissions = new List<string>();
        if (command.Permissions is { Count: > 0 })
        {
            foreach (var permission in command.Permissions)
            {
                var claim = new Claim(Permissions.ClaimType, permission);
                await _roleManager.AddClaimAsync(role, claim);
                permissions.Add(permission);
            }
        }

        _cacheInvalidator.InvalidateAll();

        return Result.Success(new RoleDto(
            role.Id,
            role.Name!,
            role.NormalizedName,
            0,
            permissions));
    }
}
