namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Handler for resending email change OTP.
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
        return await _emailChangeService.ResendOtpAsync(
            command.SessionToken,
            cancellationToken);
    }
}
