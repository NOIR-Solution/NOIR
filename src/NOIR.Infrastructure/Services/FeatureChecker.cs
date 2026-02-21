namespace NOIR.Infrastructure.Services;

/// <summary>
/// Checks whether a module/feature is effectively enabled for the current tenant.
/// Uses FusionCache (cross-request) + per-request dictionary for high performance.
/// </summary>
public sealed class FeatureChecker : IFeatureChecker, IScopedService
{
    private readonly IFusionCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly IModuleCatalog _catalog;
    private readonly IMultiTenantContextAccessor<Tenant> _tenantAccessor;
    private readonly ILogger<FeatureChecker> _logger;

    // Per-request cache (populated on first call, reused within request)
    private IReadOnlyDictionary<string, EffectiveFeatureState>? _requestCache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public FeatureChecker(
        IFusionCache cache,
        ApplicationDbContext dbContext,
        IModuleCatalog catalog,
        IMultiTenantContextAccessor<Tenant> tenantAccessor,
        ILogger<FeatureChecker> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _catalog = catalog;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        if (_catalog.IsCore(featureName))
            return true;

        var states = await LoadStatesAsync(ct);
        return states.TryGetValue(featureName, out var state) && state.IsEffective;
    }

    public async Task<EffectiveFeatureState> GetStateAsync(string featureName, CancellationToken ct = default)
    {
        var states = await LoadStatesAsync(ct);
        if (states.TryGetValue(featureName, out var state))
            return state;

        // Unknown feature: fail closed — do not assume enabled
        _logger.LogWarning("GetStateAsync called for unknown feature '{Feature}'", featureName);
        return new EffectiveFeatureState(false, false, false, false);
    }

    public async Task<IReadOnlyDictionary<string, EffectiveFeatureState>> GetAllStatesAsync(
        CancellationToken ct = default) => await LoadStatesAsync(ct);

    private async Task<IReadOnlyDictionary<string, EffectiveFeatureState>> LoadStatesAsync(
        CancellationToken ct)
    {
        if (_requestCache is not null)
            return _requestCache;

        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId))
        {
            _requestCache = BuildDefaultStates();
            return _requestCache;
        }

        var cacheKey = CacheKeys.TenantFeatures(tenantId);
        var dbOverrides = await _cache.GetOrSetAsync(
            cacheKey,
            async token => await LoadFromDbAsync(tenantId, token),
            options => options.SetDuration(CacheDuration),
            ct);

        _requestCache = ResolveEffectiveStates(dbOverrides ?? new Dictionary<string, TenantModuleStateRow>(StringComparer.OrdinalIgnoreCase));
        return _requestCache;
    }

    private async Task<Dictionary<string, TenantModuleStateRow>> LoadFromDbAsync(
        string tenantId, CancellationToken ct)
    {
        return await _dbContext.Set<TenantModuleState>()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .TagWith("FeatureChecker:LoadTenantStates")
            .ToDictionaryAsync(
                x => x.FeatureName,
                x => new TenantModuleStateRow(x.IsAvailable, x.IsEnabled),
                StringComparer.OrdinalIgnoreCase,
                ct);
    }

    private IReadOnlyDictionary<string, EffectiveFeatureState> ResolveEffectiveStates(
        Dictionary<string, TenantModuleStateRow> dbOverrides)
    {
        var result = new Dictionary<string, EffectiveFeatureState>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in _catalog.GetAllModules())
        {
            var moduleState = ResolveModuleState(module.Name, module.IsCore, module.DefaultEnabled, dbOverrides);
            result[module.Name] = moduleState;

            foreach (var feature in module.Features)
            {
                var featureState = ResolveFeatureState(feature.Name, feature.DefaultEnabled, moduleState, dbOverrides);
                result[feature.Name] = featureState;
            }
        }

        return result;
    }

    private static EffectiveFeatureState ResolveModuleState(
        string name, bool isCore, bool defaultEnabled,
        Dictionary<string, TenantModuleStateRow> overrides)
    {
        if (isCore)
            return new(true, true, true, true);

        var hasOverride = overrides.TryGetValue(name, out var row);
        var isAvailable = hasOverride ? row!.IsAvailable : true;
        var isEnabled = hasOverride ? row!.IsEnabled : defaultEnabled;
        var isEffective = isAvailable && isEnabled;

        return new(isAvailable, isEnabled, isEffective, false);
    }

    private static EffectiveFeatureState ResolveFeatureState(
        string name, bool defaultEnabled,
        EffectiveFeatureState parentState,
        Dictionary<string, TenantModuleStateRow> overrides)
    {
        if (!parentState.IsEffective)
            return new(parentState.IsAvailable, false, false, false);

        var hasOverride = overrides.TryGetValue(name, out var row);
        var isAvailable = hasOverride ? row!.IsAvailable : true;
        var isEnabled = hasOverride ? row!.IsEnabled : defaultEnabled;
        var isEffective = isAvailable && isEnabled && parentState.IsEffective;

        return new(isAvailable, isEnabled, isEffective, false);
    }

    private IReadOnlyDictionary<string, EffectiveFeatureState> BuildDefaultStates()
    {
        var result = new Dictionary<string, EffectiveFeatureState>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in _catalog.GetAllModules())
        {
            result[module.Name] = new(true, module.DefaultEnabled, module.DefaultEnabled, module.IsCore);
            foreach (var feature in module.Features)
                result[feature.Name] = new(true, feature.DefaultEnabled, feature.DefaultEnabled, false);
        }
        return result;
    }

    private sealed record TenantModuleStateRow(bool IsAvailable, bool IsEnabled);
}
