namespace NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

/// <summary>
/// Wolverine handler for updating the current user's profile.
/// Demonstrates IAuditableCommand usage for DTO-level diff tracking.
/// </summary>
public class UpdateUserProfileCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public UpdateUserProfileCommandHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        // Check if user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure<UserProfileDto>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        // Find user
        var user = await _userIdentityService.FindByIdAsync(_currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileDto>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Check for changes
        var hasChanges = false;
        var updates = new UpdateUserDto();

        if (command.FirstName is not null && command.FirstName != user.FirstName)
        {
            updates = updates with { FirstName = command.FirstName };
            hasChanges = true;
        }

        if (command.LastName is not null && command.LastName != user.LastName)
        {
            updates = updates with { LastName = command.LastName };
            hasChanges = true;
        }

        if (command.DisplayName is not null && command.DisplayName != user.DisplayName)
        {
            updates = updates with { DisplayName = command.DisplayName };
            hasChanges = true;
        }

        if (command.PhoneNumber is not null && command.PhoneNumber != user.PhoneNumber)
        {
            updates = updates with { PhoneNumber = command.PhoneNumber };
            hasChanges = true;
        }

        if (hasChanges)
        {
            var result = await _userIdentityService.UpdateUserAsync(_currentUser.UserId, updates, cancellationToken);
            if (!result.Succeeded)
            {
                return Result.Failure<UserProfileDto>(
                    Error.Failure(ErrorCodes.System.DatabaseError, _localization["auth.user.updateFailed"]));
            }

            // Refresh user data after update
            user = await _userIdentityService.FindByIdAsync(_currentUser.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<UserProfileDto>(
                    Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
            }
        }

        // Get updated roles
        var roles = await _userIdentityService.GetRolesAsync(_currentUser.UserId, cancellationToken);

        var fullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
        var dto = new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            string.IsNullOrEmpty(fullName) ? user.Email : fullName,
            user.PhoneNumber,
            user.AvatarUrl,
            roles,
            _currentUser.TenantId,
            user.IsActive,
            user.CreatedAt,
            user.ModifiedAt);

        return Result.Success(dto);
    }
}
