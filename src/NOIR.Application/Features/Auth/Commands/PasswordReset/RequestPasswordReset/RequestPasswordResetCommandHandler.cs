namespace NOIR.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Wolverine handler for initiating a password reset request.
/// Sends OTP to the user's email address for verification.
/// </summary>
public class RequestPasswordResetCommandHandler
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ILocalizationService _localization;

    public RequestPasswordResetCommandHandler(
        IPasswordResetService passwordResetService,
        ILocalizationService localization)
    {
        _passwordResetService = passwordResetService;
        _localization = localization;
    }

    public async Task<Result<PasswordResetRequestResult>> Handle(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        // Check rate limiting
        if (await _passwordResetService.IsRateLimitedAsync(command.Email, cancellationToken))
        {
            return Result.Failure<PasswordResetRequestResult>(
                Error.TooManyRequests(
                    _localization["auth.passwordReset.rateLimited"],
                    ErrorCodes.Auth.TooManyRequests));
        }

        // Request password reset (sends OTP to email)
        var result = await _passwordResetService.RequestPasswordResetAsync(
            command.Email,
            command.TenantId,
            command.IpAddress,
            cancellationToken);

        return result;
    }
}
