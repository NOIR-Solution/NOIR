namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Runs a per-tenant action only for tenants that have the specified feature enabled.
/// </summary>
public interface ITenantJobRunner
{
    /// <summary>
    /// Iterates all active tenants, sets tenant context, checks if the specified feature is enabled,
    /// and runs the action for each enabled tenant.
    /// </summary>
    Task RunForEnabledTenantsAsync(
        string featureName,
        Func<string, CancellationToken, Task> action,
        CancellationToken ct = default);
}
