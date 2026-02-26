namespace NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;

/// <summary>
/// Validator for SetModuleAvailabilityCommand.
/// </summary>
public class SetModuleAvailabilityCommandValidator : AbstractValidator<SetModuleAvailabilityCommand>
{
    public SetModuleAvailabilityCommandValidator(IModuleCatalog catalog)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.FeatureName)
            .NotEmpty().WithMessage("Feature name is required.")
            .Must(name => catalog.Exists(name))
            .WithMessage("Feature not found in catalog.")
            .Must(name => !catalog.IsCore(name))
            .WithMessage("Core modules cannot be modified.");
    }
}
