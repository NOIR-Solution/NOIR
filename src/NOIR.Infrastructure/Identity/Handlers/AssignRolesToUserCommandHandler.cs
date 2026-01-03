namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for assigning roles to a user.
/// </summary>
public class AssignRolesToUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;
    private readonly ILocalizationService _localization;

    public AssignRolesToUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
        _localization = localization;
    }

    public async Task<Result<UserDto>> Handle(AssignRolesToUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound(_localization["auth.user.notFound"]));
        }

        // Validate all roles exist
        foreach (var roleName in command.RoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Result.Failure<UserDto>(Error.NotFound(_localization["auth.role.notFound"]));
            }
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Remove roles that are not in the new list
        var rolesToRemove = currentRoles.Except(command.RoleNames).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return Result.Failure<UserDto>(Error.Failure("User.RemoveRolesFailed", _localization["auth.user.removeRolesFailed"]));
            }
        }

        // Add roles that are not in the current list
        var rolesToAdd = command.RoleNames.Except(currentRoles).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return Result.Failure<UserDto>(Error.Failure("User.AddRolesFailed", _localization["auth.user.addRolesFailed"]));
            }
        }

        _cacheInvalidator.InvalidateUser(user.Id);

        var roles = await _userManager.GetRolesAsync(user);

        return Result.Success(new UserDto(
            user.Id,
            user.Email!,
            user.UserName,
            user.DisplayName,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.LockoutEnabled,
            user.LockoutEnd,
            roles.ToList()));
    }
}
