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
