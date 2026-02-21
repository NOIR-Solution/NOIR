namespace NOIR.Domain.Interfaces;

/// <summary>
/// Checks whether a module/feature is effectively enabled for the current tenant.
/// </summary>
public interface IFeatureChecker
{
    /// <summary>Returns true if the feature is effectively enabled (available AND enabled AND parent enabled).</summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
    /// <summary>Returns detailed state for a single feature.</summary>
    Task<EffectiveFeatureState> GetStateAsync(string featureName, CancellationToken ct = default);
    /// <summary>Returns all feature states for the current tenant (used by frontend API).</summary>
    Task<IReadOnlyDictionary<string, EffectiveFeatureState>> GetAllStatesAsync(CancellationToken ct = default);
}

/// <summary>
/// Resolved state for a single module/feature.
/// </summary>
public sealed record EffectiveFeatureState(
    bool IsAvailable,   // Platform admin has made this available
    bool IsEnabled,     // Tenant admin has enabled this
    bool IsEffective,   // Available AND Enabled AND ParentEffective
    bool IsCore         // Core modules are always effective
);
