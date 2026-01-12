namespace NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

/// <summary>
/// Validator for UpdateUserProfileCommand.
/// Limits harmonized with UpdateUserCommandValidator (50 chars for first/last name).
/// </summary>
public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private const int MaxNameLength = 50;
    private const int MaxDisplayNameLength = 100;
    private const int MaxPhoneLength = 20;

    public UpdateUserProfileCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(MaxNameLength)
            .When(x => x.FirstName is not null)
            .WithMessage(localization.Get("validation.firstName.maxLength", MaxNameLength));

        RuleFor(x => x.LastName)
            .MaximumLength(MaxNameLength)
            .When(x => x.LastName is not null)
            .WithMessage(localization.Get("validation.lastName.maxLength", MaxNameLength));

        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength)
            .When(x => x.DisplayName is not null)
            .WithMessage(localization.Get("validation.displayName.maxLength", MaxDisplayNameLength));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(MaxPhoneLength)
            .When(x => x.PhoneNumber is not null)
            .WithMessage(localization.Get("validation.phoneNumber.maxLength", MaxPhoneLength));
    }
}
