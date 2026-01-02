namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for assigning roles to a user.
/// </summary>
public class AssignRolesToUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionCacheInvalidator _cacheInvalidator;

    public AssignRolesToUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IPermissionCacheInvalidator cacheInvalidator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Result<UserDto>> Handle(AssignRolesToUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User", command.UserId));
        }

        // Validate all roles exist
        foreach (var roleName in command.RoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Result.Failure<UserDto>(Error.NotFound($"Role '{roleName}' does not exist"));
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
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return Result.Failure<UserDto>(Error.Failure("User.RemoveRolesFailed", $"Failed to remove roles: {errors}"));
            }
        }

        // Add roles that are not in the current list
        var rolesToAdd = command.RoleNames.Except(currentRoles).ToList();
        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result.Failure<UserDto>(Error.Failure("User.AddRolesFailed", $"Failed to add roles: {errors}"));
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
