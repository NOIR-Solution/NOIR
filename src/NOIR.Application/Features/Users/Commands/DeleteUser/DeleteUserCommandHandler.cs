namespace NOIR.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Wolverine handler for soft-deleting a user.
/// Marks the user as deleted/disabled rather than removing from database.
/// </summary>
public class DeleteUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public DeleteUserCommandHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userIdentityService.FindByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Prevent deleting system users
        if (user.IsSystemUser)
        {
            return Result.Failure<bool>(
                Error.Validation("userId", _localization["users.cannotModifySystemUser"], ErrorCodes.Business.CannotDelete));
        }

        // Prevent self-deletion
        if (_currentUser.UserId == command.UserId)
        {
            return Result.Failure<bool>(
                Error.Validation("userId", _localization["users.cannotDeleteSelf"], ErrorCodes.Business.CannotDelete));
        }

        // Soft delete user
        var deletedBy = _currentUser.UserId ?? "system";
        var result = await _userIdentityService.SoftDeleteUserAsync(command.UserId, deletedBy, cancellationToken);

        if (!result.Succeeded)
        {
            return Result.Failure<bool>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        return Result.Success(true);
    }
}
