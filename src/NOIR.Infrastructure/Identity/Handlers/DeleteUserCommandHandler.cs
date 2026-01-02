namespace NOIR.Infrastructure.Identity.Handlers;

/// <summary>
/// Wolverine handler for soft-deleting a user by locking them out permanently.
/// </summary>
public class DeleteUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<bool>(Error.NotFound("User", command.UserId));
        }

        // Prevent self-deletion
        if (user.Id == _currentUser.UserId)
        {
            return Result.Failure<bool>(Error.Validation("UserId", "You cannot delete your own account"));
        }

        // Prevent deletion of admin users
        var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
        if (isAdmin)
        {
            return Result.Failure<bool>(Error.Validation("UserId", "Cannot delete users with Admin role"));
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
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(Error.Failure("User.DeleteFailed", $"Failed to delete user: {errors}"));
        }

        return Result.Success(true);
    }
}
