namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for soft-deleting a user by locking them out permanently.
/// </summary>
public class DeleteUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly ILocalizationService _localization;

    public DeleteUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser,
        ILocalizationService localization)
    {
        _userManager = userManager;
        _currentUser = currentUser;
        _localization = localization;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<bool>(
                Error.NotFound(_localization["auth.user.notFound"], ErrorCodes.Auth.UserNotFound));
        }

        // Prevent self-deletion
        if (user.Id == _currentUser.UserId)
        {
            return Result.Failure<bool>(
                Error.Validation("UserId", _localization["auth.user.deleteSelf"], ErrorCodes.Business.CannotDelete));
        }

        // Prevent deletion of admin users
        var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
        if (isAdmin)
        {
            return Result.Failure<bool>(
                Error.Validation("UserId", _localization["auth.user.deleteAdmin"], ErrorCodes.Business.CannotDelete));
        }

        // Soft delete: lock out permanently
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        // Optionally mark as deleted
        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result.Failure<bool>(Error.Failure(ErrorCodes.System.DatabaseError, _localization["auth.user.deleteFailed"]));
        }

        return Result.Success(true);
    }
}
