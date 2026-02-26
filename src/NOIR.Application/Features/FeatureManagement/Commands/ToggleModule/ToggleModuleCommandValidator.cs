namespace NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;

/// <summary>
/// Validator for ToggleModuleCommand.
/// </summary>
public class ToggleModuleCommandValidator : AbstractValidator<ToggleModuleCommand>
{
    public ToggleModuleCommandValidator(IModuleCatalog catalog)
    {
        RuleFor(x => x.FeatureName)
            .NotEmpty().WithMessage("Feature name is required.")
            .Must(name => catalog.Exists(name))
            .WithMessage("Feature not found in catalog.")
            .Must(name => !catalog.IsCore(name))
            .WithMessage("Core modules cannot be toggled.");
    }
}
