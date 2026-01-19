namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Wolverine handler for verifying an email change OTP and completing the email update.
/// </summary>
public class VerifyEmailChangeCommandHandler
{
    private readonly IEmailChangeService _emailChangeService;

    public VerifyEmailChangeCommandHandler(IEmailChangeService emailChangeService)
    {
        _emailChangeService = emailChangeService;
    }

    public async Task<Result<EmailChangeVerifyResult>> Handle(
        VerifyEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        // Verify OTP and complete email change
        var result = await _emailChangeService.VerifyOtpAsync(
            command.SessionToken,
            command.Otp,
            cancellationToken);

        return result;
    }
}
