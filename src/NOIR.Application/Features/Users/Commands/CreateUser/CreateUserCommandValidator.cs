namespace NOIR.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private const int MaxEmailLength = 256;
    private const int MinPasswordLength = 6;
    private const int MaxPasswordLength = 100;
    private const int MaxDisplayNameLength = 100;
    private const int MaxNameLength = 50;

    public CreateUserCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(localization["validation.email.required"])
            .MaximumLength(MaxEmailLength).WithMessage(localization.Get("validation.email.maxLength", MaxEmailLength))
            .EmailAddress().WithMessage(localization["validation.email.invalid"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(localization["validation.password.required"])
            .MinimumLength(MinPasswordLength).WithMessage(localization.Get("validation.password.minLength", MinPasswordLength))
            .MaximumLength(MaxPasswordLength).WithMessage(localization.Get("validation.password.maxLength", MaxPasswordLength));

        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength).WithMessage(localization.Get("validation.displayName.maxLength", MaxDisplayNameLength))
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.FirstName)
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.firstName.maxLength", MaxNameLength))
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.lastName.maxLength", MaxNameLength))
            .When(x => x.LastName is not null);
    }
}
