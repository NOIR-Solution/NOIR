namespace NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;

/// <summary>
/// Command to toggle a module on/off for the current tenant (tenant admin operation).
/// </summary>
public sealed record ToggleModuleCommand(
    string FeatureName,
    bool IsEnabled
) : IAuditableCommand<TenantFeatureStateDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => FeatureName;
    public string? GetTargetDisplayName() => FeatureName;
    public string? GetActionDescription() => IsEnabled
        ? $"Enabled '{FeatureName}'"
        : $"Disabled '{FeatureName}'";
}

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
