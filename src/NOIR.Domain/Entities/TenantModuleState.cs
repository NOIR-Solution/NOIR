namespace NOIR.Domain.Entities;

/// <summary>
/// Stores per-tenant overrides for module/feature state.
/// Platform admin controls IsAvailable, tenant admin controls IsEnabled.
/// </summary>
public class TenantModuleState : TenantEntity<Guid>
{
    /// <summary>Module or feature name (e.g., "Ecommerce" or "Ecommerce.Reviews")</summary>
    public string FeatureName { get; private set; } = default!;

    /// <summary>Platform admin controls this. True = available to tenant.</summary>
    public bool IsAvailable { get; private set; } = true;

    /// <summary>Tenant admin controls this. True = enabled by tenant.</summary>
    public bool IsEnabled { get; private set; } = true;

    private TenantModuleState() { } // EF Core

    public static TenantModuleState Create(string featureName)
        => new() { Id = Guid.NewGuid(), FeatureName = featureName };

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;
    public void SetEnabled(bool isEnabled) => IsEnabled = isEnabled;
}
