namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResetPassword;

/// <summary>
/// Wolverine handler for resetting a user's password using a valid reset token.
/// </summary>
public class ResetPasswordCommandHandler
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILocalizationService _localization;

    public ResetPasswordCommandHandler(
        IPasswordResetService passwordResetService,
        ILocalizationService localization)
    {
        _passwordResetService = passwordResetService;
        _localization = localization;
    }

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Reset password using the token
        var result = await _passwordResetService.ResetPasswordAsync(
            command.ResetToken,
            command.NewPassword,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<ResetPasswordResult>(result.Error);
        }

        return Result.Success(new ResetPasswordResult(
            true,
            _localization["auth.passwordReset.success"]));
    }
}

/// <summary>
/// Result of the password reset operation.
/// </summary>
public record ResetPasswordResult(bool Success, string Message);
