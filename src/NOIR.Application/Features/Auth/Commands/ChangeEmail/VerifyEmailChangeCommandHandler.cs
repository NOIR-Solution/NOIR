namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Handler for verifying email change OTP and completing the email update.
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
        return await _emailChangeService.VerifyOtpAsync(
            command.SessionToken,
            command.Otp,
            cancellationToken);
    }
}
