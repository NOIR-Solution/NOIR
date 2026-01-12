namespace NOIR.Application.Features.Notifications.Commands.UpdatePreferences;

/// <summary>
/// Validator for UpdatePreferencesCommand.
/// </summary>
public class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences)
            .NotNull()
            .WithMessage("Preferences list is required.")
            .NotEmpty()
            .WithMessage("At least one preference update is required.");

        RuleForEach(x => x.Preferences)
            .ChildRules(pref =>
            {
                pref.RuleFor(p => p.Category)
                    .IsInEnum()
                    .WithMessage("Invalid notification category.");

                pref.RuleFor(p => p.EmailFrequency)
                    .IsInEnum()
                    .WithMessage("Invalid email frequency.");
            });
    }
}
