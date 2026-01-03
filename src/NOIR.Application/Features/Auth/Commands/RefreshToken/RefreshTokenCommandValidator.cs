namespace NOIR.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage(localization["validation.accessToken.required"]);

        // RefreshToken can be null if UseCookies is true (will be read from cookie)
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .When(x => !x.UseCookies)
            .WithMessage(localization["validation.refreshToken.required"]);
    }
}
