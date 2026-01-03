namespace NOIR.Application.Features.Auth.Commands.Login;

/// <summary>
/// Validator for LoginCommand.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(localization["validation.email.required"])
            .EmailAddress().WithMessage(localization["validation.email.invalid"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localization["validation.password.required"]);
    }
}
