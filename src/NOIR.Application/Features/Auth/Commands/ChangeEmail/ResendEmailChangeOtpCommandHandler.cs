namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Wolverine handler for resending an email change OTP.
/// </summary>
public class ResendEmailChangeOtpCommandHandler
{
    private readonly IEmailChangeService _emailChangeService;

    public ResendEmailChangeOtpCommandHandler(IEmailChangeService emailChangeService)
    {
        _emailChangeService = emailChangeService;
    }

    public async Task<Result<EmailChangeResendResult>> Handle(
        ResendEmailChangeOtpCommand command,
        CancellationToken cancellationToken)
    {
        // Resend OTP for the existing session
        var result = await _emailChangeService.ResendOtpAsync(
            command.SessionToken,
            cancellationToken);

        return result;
    }
}
