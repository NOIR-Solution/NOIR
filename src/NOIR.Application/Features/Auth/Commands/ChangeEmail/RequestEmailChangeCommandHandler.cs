namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Wolverine handler for initiating an email change request.
/// Sends OTP to the new email address for verification.
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
        // Verify user is authenticated
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.Unauthorized(
                    _localization["auth.user.notAuthenticated"],
                    ErrorCodes.Auth.Unauthorized));
        }

        // Check rate limiting
        if (await _emailChangeService.IsRateLimitedAsync(command.UserId, cancellationToken))
        {
            return Result.Failure<EmailChangeRequestResult>(
                Error.TooManyRequests(
                    _localization["auth.emailChange.rateLimited"],
                    ErrorCodes.Auth.TooManyRequests));
        }

        // Request email change (sends OTP to new email)
        var result = await _emailChangeService.RequestEmailChangeAsync(
            command.UserId,
            command.NewEmail,
            command.TenantId,
            command.IpAddress,
            cancellationToken);

        return result;
    }
}
