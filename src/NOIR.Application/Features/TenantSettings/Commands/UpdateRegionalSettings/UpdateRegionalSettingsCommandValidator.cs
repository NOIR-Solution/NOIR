namespace NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;

/// <summary>
/// Validator for UpdateRegionalSettingsCommand.
/// </summary>
public class UpdateRegionalSettingsCommandValidator : AbstractValidator<UpdateRegionalSettingsCommand>
{
    private static readonly HashSet<string> ValidLanguages = ["en", "vi", "ja", "ko", "zh", "fr", "de", "es", "it", "pt"];
    private static readonly HashSet<string> ValidDateFormats = ["YYYY-MM-DD", "MM/DD/YYYY", "DD/MM/YYYY", "DD.MM.YYYY"];

    public UpdateRegionalSettingsCommandValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .WithMessage("Timezone is required.")
            .MaximumLength(100)
            .WithMessage("Timezone must not exceed 100 characters.");

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required.")
            .Must(lang => ValidLanguages.Contains(lang))
            .WithMessage("Language must be a supported language code.");

        RuleFor(x => x.DateFormat)
            .NotEmpty()
            .WithMessage("Date format is required.")
            .Must(fmt => ValidDateFormats.Contains(fmt))
            .WithMessage("Date format must be one of: YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY, DD.MM.YYYY.");
    }
}
