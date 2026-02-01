namespace NOIR.Application.Features.Users.Commands.LockUser;

/// <summary>
/// Wolverine handler for locking/unlocking a user account.
/// </summary>
public class LockUserCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public LockUserCommandHandler(
        IUserIdentityService userIdentityService,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userIdentityService = userIdentityService;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<bool>> Handle(LockUserCommand command, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userIdentityService.FindByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(_localization["users.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Prevent self-locking
        if (_currentUser.UserId == command.TargetUserId)
        {
            return Result.Failure<bool>(
                Error.Validation("userId", _localization["users.cannotLockSelf"], ErrorCodes.Business.CannotModify));
        }

        // Lock or unlock user
        var lockedBy = command.Lock ? (_currentUser.UserId ?? "system") : null;
        var result = await _userIdentityService.SetUserLockoutAsync(
            command.TargetUserId,
            command.Lock,
            lockedBy,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Result.Failure<bool>(
                Error.ValidationErrors(result.Errors!, ErrorCodes.Validation.General));
        }

        return Result.Success(true);
    }
}
