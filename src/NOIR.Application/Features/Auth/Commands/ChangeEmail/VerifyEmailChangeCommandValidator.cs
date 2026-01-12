namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Validator for VerifyEmailChangeCommand.
/// </summary>
public class VerifyEmailChangeCommandValidator : AbstractValidator<VerifyEmailChangeCommand>
{
    public VerifyEmailChangeCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);

        RuleFor(x => x.Otp)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .Length(6)
            .WithMessage(localization["validation.otp.length"]);
    }
}
