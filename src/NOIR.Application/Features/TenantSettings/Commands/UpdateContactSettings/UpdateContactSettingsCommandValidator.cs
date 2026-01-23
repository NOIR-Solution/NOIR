namespace NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;

/// <summary>
/// Validator for UpdateContactSettingsCommand.
/// </summary>
public class UpdateContactSettingsCommandValidator : AbstractValidator<UpdateContactSettingsCommand>
{
    public UpdateContactSettingsCommandValidator()
    {
        RuleFor(x => x.Email)
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.")
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .WithMessage("Phone must not exceed 50 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address must not exceed 500 characters.");
    }
}
