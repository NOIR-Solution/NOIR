namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for updating the current user's profile.
/// Demonstrates IAuditableCommand usage for DTO-level diff tracking.
/// </summary>
public class UpdateUserProfileCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public UpdateUserProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<UserProfileDto>(Error.Unauthorized("User is not authenticated."));
        }

        // Find user
        var user = await _userManager.FindByIdAsync(_currentUser.UserId);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>(Error.NotFound("User", _currentUser.UserId));
        }

        // Update profile fields
        var hasChanges = false;

        if (command.FirstName is not null && command.FirstName != user.FirstName)
        {
            user.FirstName = command.FirstName;
            hasChanges = true;
        }

        if (command.LastName is not null && command.LastName != user.LastName)
        {
            user.LastName = command.LastName;
            hasChanges = true;
        }

        if (hasChanges)
        {
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Result.Failure<UserProfileDto>(Error.ValidationErrors(result.Errors.Select(e => e.Description)));
            }
        }

        // Get updated roles
        var roles = await _userManager.GetRolesAsync(user);

        var dto = new UserProfileDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.FullName,
            roles,
            user.TenantId,
            user.IsActive,
            user.CreatedAt,
            user.ModifiedAt);

        return Result.Success(dto);
    }
}
