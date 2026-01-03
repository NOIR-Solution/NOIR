namespace NOIR.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private const int MaxDisplayNameLength = 100;
    private const int MaxNameLength = 50;

    public UpdateUserCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(localization["validation.userId.required"]);

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
