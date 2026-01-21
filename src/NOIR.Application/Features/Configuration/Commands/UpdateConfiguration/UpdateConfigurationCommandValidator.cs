namespace NOIR.Application.Features.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Validator for UpdateConfigurationCommand.
/// Validates section name and JSON structure.
/// </summary>
public class UpdateConfigurationCommandValidator : AbstractValidator<UpdateConfigurationCommand>
{
    public UpdateConfigurationCommandValidator()
    {
        RuleFor(x => x.SectionName)
            .NotEmpty()
            .WithMessage("Configuration section name is required.");

        RuleFor(x => x.NewValueJson)
            .NotEmpty()
            .WithMessage("Configuration value is required.")
            .Must(BeValidJson)
            .WithMessage("Configuration value must be valid JSON.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required for audit trail.");
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
