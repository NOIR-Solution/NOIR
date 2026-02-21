namespace NOIR.Infrastructure.Services;

public sealed class FeatureCacheInvalidator : IFeatureCacheInvalidator, IScopedService
{
    private readonly IFusionCache _cache;
    private readonly ILogger<FeatureCacheInvalidator> _logger;

    public FeatureCacheInvalidator(
        IFusionCache cache,
        ILogger<FeatureCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateAsync(string tenantId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.TenantFeatures(tenantId);
        await _cache.RemoveAsync(cacheKey, token: ct);
        _logger.LogInformation("Invalidated feature cache for tenant {TenantId}", tenantId);
    }
}
