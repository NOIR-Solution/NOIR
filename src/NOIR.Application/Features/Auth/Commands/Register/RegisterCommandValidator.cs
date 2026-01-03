namespace NOIR.Application.Features.Auth.Commands.Register;

/// <summary>
/// Validator for RegisterCommand.
/// </summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private const int MinPasswordLength = 6;
    private const int MaxNameLength = 100;

    public RegisterCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(localization["validation.email.required"])
            .EmailAddress().WithMessage(localization["validation.email.invalid"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localization["validation.password.required"])
            .MinimumLength(MinPasswordLength).WithMessage(localization.Get("validation.password.tooShort", MinPasswordLength));

        RuleFor(x => x.FirstName)
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.firstName.maxLength", MaxNameLength));

        RuleFor(x => x.LastName)
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.lastName.maxLength", MaxNameLength));
    }
}
