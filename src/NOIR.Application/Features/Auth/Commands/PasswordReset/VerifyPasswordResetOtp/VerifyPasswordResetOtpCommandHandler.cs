namespace NOIR.Application.Features.Auth.Commands.PasswordReset.VerifyPasswordResetOtp;

/// <summary>
/// Wolverine handler for verifying a password reset OTP.
/// Returns a reset token if the OTP is valid.
/// </summary>
public class VerifyPasswordResetOtpCommandHandler
{
    private readonly IPasswordResetService _passwordResetService;

    public VerifyPasswordResetOtpCommandHandler(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    public async Task<Result<PasswordResetVerifyResult>> Handle(
        VerifyPasswordResetOtpCommand command,
        CancellationToken cancellationToken)
    {
        // Verify OTP and get reset token
        var result = await _passwordResetService.VerifyOtpAsync(
            command.SessionToken,
            command.Otp,
            cancellationToken);

        return result;
    }
}
