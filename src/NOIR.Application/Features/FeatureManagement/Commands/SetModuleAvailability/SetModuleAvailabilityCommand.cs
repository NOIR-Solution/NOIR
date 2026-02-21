namespace NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;

/// <summary>
/// Command to set module availability for a specific tenant (platform admin operation).
/// </summary>
public sealed record SetModuleAvailabilityCommand(
    string TenantId,
    string FeatureName,
    bool IsAvailable
) : IAuditableCommand<TenantFeatureStateDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => $"{TenantId}:{FeatureName}";
    public string? GetTargetDisplayName() => FeatureName;
    public string? GetActionDescription() => IsAvailable
        ? $"Made '{FeatureName}' available"
        : $"Made '{FeatureName}' unavailable";
}

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
