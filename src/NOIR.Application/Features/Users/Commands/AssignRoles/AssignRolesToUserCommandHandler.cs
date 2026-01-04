namespace NOIR.Application.Features.Users.Commands.AssignRoles;

/// <summary>
/// Wolverine handler for assigning roles to a user.
/// Replaces existing roles with the new set of roles.
/// </summary>
public class AssignRolesToUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRoleIdentityService _roleIdentityService;
    private readonly ILocalizationService _localization;

    public AssignRolesToUserCommandHandler(
        IUserIdentityService userIdentityService,
        IRoleIdentityService roleIdentityService,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _roleIdentityService = roleIdentityService;
        _localization = localization;
    }

    public async Task<Result<UserDto>> Handle(AssignRolesToUserCommand command, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userIdentityService.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Verify all roles exist
        foreach (var roleName in command.RoleNames)
        {
            var roleExists = await _roleIdentityService.RoleExistsAsync(roleName, cancellationToken);
            if (!roleExists)
            {
                return Result.Failure<UserDto>(
                    Error.NotFound(
                        string.Format(_localization["roles.notFound"], roleName),
                        ErrorCodes.Auth.RoleNotFound));
            }
        }

        // Assign roles (replaces existing)
        var result = await _userIdentityService.AssignRolesAsync(
            command.UserId,
            command.RoleNames,
            replaceExisting: true,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Result.Failure<UserDto>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        // Return updated user with roles
        var updatedRoles = await _userIdentityService.GetRolesAsync(command.UserId, cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.Email, // UserName
            user.DisplayName,
            user.FirstName,
            user.LastName,
            true, // EmailConfirmed - assumed for existing users
            !user.IsActive, // LockoutEnabled - inverse of IsActive
            user.IsActive ? null : DateTimeOffset.MaxValue, // LockoutEnd
            updatedRoles);

        return Result.Success(userDto);
    }
}
