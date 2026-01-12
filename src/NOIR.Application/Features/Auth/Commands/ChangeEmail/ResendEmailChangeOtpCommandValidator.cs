namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Validator for ResendEmailChangeOtpCommand.
/// </summary>
public class ResendEmailChangeOtpCommandValidator : AbstractValidator<ResendEmailChangeOtpCommand>
{
    public ResendEmailChangeOtpCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.SessionToken)
            .NotEmpty()
            .WithMessage(localization["validation.required"]);
    }
}
