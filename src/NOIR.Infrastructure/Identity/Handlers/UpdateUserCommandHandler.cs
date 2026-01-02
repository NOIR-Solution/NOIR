namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for updating a user (admin operation).
/// </summary>
public class UpdateUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound("User", command.UserId));
        }

        // Update fields if provided
        if (command.DisplayName is not null)
            user.DisplayName = command.DisplayName;

        if (command.FirstName is not null)
            user.FirstName = command.FirstName;

        if (command.LastName is not null)
            user.LastName = command.LastName;

        if (command.LockoutEnabled.HasValue)
            user.LockoutEnabled = command.LockoutEnabled.Value;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<UserDto>(Error.Failure("User.UpdateFailed", $"Failed to update user: {errors}"));
        }

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
