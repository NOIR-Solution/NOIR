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
