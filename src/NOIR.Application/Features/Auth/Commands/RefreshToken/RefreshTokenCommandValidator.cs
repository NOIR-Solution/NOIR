namespace NOIR.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshTokenCommand.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required.");

        // RefreshToken can be null if UseCookies is true (will be read from cookie)
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .When(x => !x.UseCookies)
            .WithMessage("Refresh token is required when not using cookies.");
    }
}
