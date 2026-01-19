namespace NOIR.Application.Features.Auth.Commands.PasswordReset.ResendPasswordResetOtp;

/// <summary>
/// Wolverine handler for resending a password reset OTP.
/// </summary>
public class ResendPasswordResetOtpCommandHandler
{
    private readonly IPasswordResetService _passwordResetService;

    public ResendPasswordResetOtpCommandHandler(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    public async Task<Result<PasswordResetResendResult>> Handle(
        ResendPasswordResetOtpCommand command,
        CancellationToken cancellationToken)
    {
        // Resend OTP for the existing session
        var result = await _passwordResetService.ResendOtpAsync(
            command.SessionToken,
            cancellationToken);

        return result;
    }
}
