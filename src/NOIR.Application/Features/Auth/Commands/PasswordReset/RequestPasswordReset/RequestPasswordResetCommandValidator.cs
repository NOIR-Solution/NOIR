namespace NOIR.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Validator for RequestPasswordResetCommand.
/// </summary>
public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .MaximumLength(256)
            .WithMessage(localization["validation.maxLength"])
            .EmailAddress()
            .WithMessage(localization["validation.email"]);
    }
}
