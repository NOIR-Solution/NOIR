namespace NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Validator for RequestEmailChangeCommand.
/// </summary>
public class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .WithMessage(localization["validation.required"])
            .MaximumLength(256)
            .WithMessage(localization["validation.maxLength"])
            .EmailAddress()
            .WithMessage(localization["validation.email"]);
    }
}
