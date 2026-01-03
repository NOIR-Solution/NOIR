namespace NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

/// <summary>
/// Validator for UpdateUserProfileCommand.
/// Limits harmonized with UpdateUserCommandValidator (50 chars for first/last name).
/// </summary>
public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    private const int MaxNameLength = 50;

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
    }
}
