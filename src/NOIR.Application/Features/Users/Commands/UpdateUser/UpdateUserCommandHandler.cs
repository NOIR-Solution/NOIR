namespace NOIR.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Wolverine handler for admin user updates.
/// Allows administrators to update any user's profile information.
/// </summary>
public class UpdateUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateUserCommandHandler(
        IUserIdentityService userIdentityService,
        ILocalizationService localization,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _userIdentityService = userIdentityService;
        _localization = localization;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        // Verify user exists
        var existingUser = await _userIdentityService.FindByIdAsync(command.TargetUserId, cancellationToken);
        if (existingUser is null)
        {
            return Result.Failure<UserDto>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Prevent locking system users (LockoutEnabled = true means IsActive = false)
        if (existingUser.IsSystemUser && command.LockoutEnabled.HasValue && command.LockoutEnabled.Value)
        {
            return Result.Failure<UserDto>(
                Error.Validation("lockoutEnabled", _localization["users.cannotLockSystemUser"], ErrorCodes.Business.CannotModify));
        }

        // Prepare update DTO
        var updateDto = new UpdateUserDto(
            FirstName: command.FirstName,
            LastName: command.LastName,
            DisplayName: command.DisplayName,
            IsActive: command.LockoutEnabled.HasValue ? !command.LockoutEnabled.Value : null);

        // Update user
        var result = await _userIdentityService.UpdateUserAsync(command.TargetUserId, updateDto, cancellationToken);
        if (!result.Succeeded)
        {
            return Result.Failure<UserDto>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        // Get updated user
        var updatedUser = await _userIdentityService.FindByIdAsync(command.TargetUserId, cancellationToken);
        if (updatedUser is null)
        {
            return Result.Failure<UserDto>(
                Error.Failure(ErrorCodes.System.UnknownError, "Failed to retrieve updated user"));
        }

        // Get roles
        var roles = await _userIdentityService.GetRolesAsync(command.TargetUserId, cancellationToken);

        var userDto = new UserDto(
            updatedUser.Id,
            updatedUser.Email,
            updatedUser.Email, // UserName
            updatedUser.DisplayName,
            updatedUser.FirstName,
            updatedUser.LastName,
            true, // EmailConfirmed - assumed for existing users
            !updatedUser.IsActive, // LockoutEnabled - inverse of IsActive
            updatedUser.IsActive ? null : DateTimeOffset.MaxValue, // LockoutEnd
            roles);

        if (Guid.TryParse(command.TargetUserId, out var userGuid))
        {
            await _entityUpdateHub.PublishEntityUpdatedAsync(
                entityType: "User",
                entityId: userGuid,
                operation: EntityOperation.Updated,
                tenantId: _currentUser.TenantId!,
                ct: cancellationToken);
        }

        return Result.Success(userDto);
    }
}
