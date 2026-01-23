namespace NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;

/// <summary>
/// Validator for UpdateBrandingSettingsCommand.
/// </summary>
public class UpdateBrandingSettingsCommandValidator : AbstractValidator<UpdateBrandingSettingsCommand>
{
    public UpdateBrandingSettingsCommandValidator()
    {
        RuleFor(x => x.LogoUrl)
            .MaximumLength(2000)
            .WithMessage("Logo URL must not exceed 2000 characters.");

        RuleFor(x => x.FaviconUrl)
            .MaximumLength(2000)
            .WithMessage("Favicon URL must not exceed 2000 characters.");

        RuleFor(x => x.PrimaryColor)
            .MaximumLength(50)
            .WithMessage("Primary color must not exceed 50 characters.")
            .Matches(@"^(#[0-9A-Fa-f]{6}|#[0-9A-Fa-f]{3})?$")
            .When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage("Primary color must be a valid hex color (e.g., #FF5733).");

        RuleFor(x => x.SecondaryColor)
            .MaximumLength(50)
            .WithMessage("Secondary color must not exceed 50 characters.")
            .Matches(@"^(#[0-9A-Fa-f]{6}|#[0-9A-Fa-f]{3})?$")
            .When(x => !string.IsNullOrEmpty(x.SecondaryColor))
            .WithMessage("Secondary color must be a valid hex color (e.g., #FF5733).");
    }
}
