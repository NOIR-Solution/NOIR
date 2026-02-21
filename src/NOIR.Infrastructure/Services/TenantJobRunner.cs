namespace NOIR.Infrastructure.Services;

/// <summary>
/// Runs a per-tenant action only for tenants that have the specified feature enabled.
/// Used by background jobs (Hangfire) to gate per-tenant processing on feature state.
/// </summary>
public sealed class TenantJobRunner : ITenantJobRunner, IScopedService
{
    private readonly TenantStoreDbContext _tenantStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantJobRunner> _logger;

    public TenantJobRunner(
        TenantStoreDbContext tenantStore,
        IServiceScopeFactory scopeFactory,
        ILogger<TenantJobRunner> logger)
    {
        _tenantStore = tenantStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunForEnabledTenantsAsync(
        string featureName,
        Func<string, CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        // Load all active tenants
        var tenants = await _tenantStore.TenantInfo
            .Where(t => t.IsActive && !t.IsDeleted)
            .TagWith("TenantJobRunner:LoadActiveTenants")
            .ToListAsync(ct);

        _logger.LogInformation(
            "TenantJobRunner: Processing {Count} active tenants for feature '{Feature}'",
            tenants.Count, featureName);

        foreach (var tenant in tenants)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            // Set Finbuckle tenant context for this scope
            var tenantSetter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);

            var featureChecker = scope.ServiceProvider.GetRequiredService<IFeatureChecker>();

            if (!await featureChecker.IsEnabledAsync(featureName, ct))
            {
                _logger.LogDebug(
                    "Skipping tenant {TenantId} ({TenantName}) — feature '{Feature}' not enabled",
                    tenant.Id, tenant.Name, featureName);
                continue;
            }

            try
            {
                _logger.LogDebug(
                    "Running job for tenant {TenantId} ({TenantName}), feature '{Feature}'",
                    tenant.Id, tenant.Name, featureName);

                await action(tenant.Id!, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Job failed for tenant {TenantId} ({TenantName}) with feature '{Feature}'",
                    tenant.Id, tenant.Name, featureName);
                // Continue to next tenant — don't let one failure stop all tenants
            }
        }
    }
}
