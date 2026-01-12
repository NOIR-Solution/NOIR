namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Handler for initiating email change request.
/// </summary>
public class RequestEmailChangeCommandHandler
{
    private readonly IEmailChangeService _emailChangeService;
    private readonly ILocalizationService _localization;

    public RequestEmailChangeCommandHandler(
        IEmailChangeService emailChangeService,
        ILocalizationService localization)
    {
        _emailChangeService = emailChangeService;
        _localization = localization;
    }

    public async Task<Result<EmailChangeRequestResult>> Handle(
        RequestEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.Unauthorized(_localization["auth.user.notAuthenticated"], ErrorCodes.Auth.Unauthorized));
        }

        return await _emailChangeService.RequestEmailChangeAsync(
            command.UserId,
            command.NewEmail,
            command.TenantId,
            command.IpAddress,
            cancellationToken);
    }
}
