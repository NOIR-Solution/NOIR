namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for updating a user (admin operation).
/// </summary>
public class UpdateUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILocalizationService _localization;

    public UpdateUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _localization = localization;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<UserDto>(Error.NotFound(_localization["auth.user.notFound"]));
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
            return Result.Failure<UserDto>(Error.Failure("User.UpdateFailed", _localization["auth.user.updateFailed"]));
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
