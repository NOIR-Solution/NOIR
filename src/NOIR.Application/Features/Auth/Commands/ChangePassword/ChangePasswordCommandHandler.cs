namespace NOIR.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// Wolverine handler for changing user password.
/// Verifies current password, sets new password, and revokes all sessions for security.
/// </summary>
public class ChangePasswordCommandHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILocalizationService _localization;

    public ChangePasswordCommandHandler(
        ICurrentUser currentUser,
        IUserIdentityService userIdentityService,
        IRefreshTokenService refreshTokenService,
        ILocalizationService localization)
    {
        _currentUser = currentUser;
        _userIdentityService = userIdentityService;
        _refreshTokenService = refreshTokenService;
        _localization = localization;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // Verify user is authenticated
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
        {
            return Result.Failure(
                Error.Unauthorized(
                    _localization["auth.user.notAuthenticated"],
                    ErrorCodes.Auth.Unauthorized));
        }

        var userId = _currentUser.UserId;

        // Change password (internally verifies current password)
        var result = await _userIdentityService.ChangePasswordAsync(
            userId,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            // Check for specific error patterns from ASP.NET Identity
            var errors = result.Errors ?? ["Password change failed."];
            var errorMessage = string.Join(" ", errors);

            // ASP.NET Identity returns "Incorrect password." for wrong current password
            if (errorMessage.Contains("Incorrect password", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(
                    Error.Unauthorized(
                        _localization["auth.changePassword.incorrectCurrentPassword"],
                        ErrorCodes.Auth.InvalidPassword));
            }

            return Result.Failure(
                Error.ValidationErrors(errors, ErrorCodes.Validation.General));
        }

        // Revoke all refresh tokens for security (force re-login on all devices)
        await _refreshTokenService.RevokeAllUserTokensAsync(
            userId,
            reason: "Password changed - all sessions revoked for security",
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
