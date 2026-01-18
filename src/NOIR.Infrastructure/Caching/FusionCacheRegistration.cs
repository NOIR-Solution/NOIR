namespace NOIR.Infrastructure.Caching;

using ZiggyCreatures.Caching.Fusion;

/// <summary>
/// FusionCache DI registration.
/// Provides L1 (in-memory) + optional L2 (distributed) hybrid caching
/// with stampede protection, fail-safe, and soft/hard timeouts.
/// </summary>
public static class FusionCacheRegistration
{
    /// <summary>
    /// Adds FusionCache services to the DI container.
    /// Default: In-memory only (L1 cache) - no Redis required.
    /// Optional: Add Redis for L2 distributed cache and backplane.
    /// </summary>
    public static IServiceCollection AddFusionCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>()
            ?? new CacheSettings();

        // Configure FusionCache options
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        // Register FusionCache with default options
        // DEFAULT: In-memory only (L1 cache)
        // This already gives you:
        // - Stampede protection (process-level)
        // - Fail-safe (return stale data)
        // - Soft/hard timeouts
        // - Tag-based invalidation (v2)
        services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromMinutes(settings.DefaultExpirationMinutes);
                options.FailSafeMaxDuration = TimeSpan.FromMinutes(settings.FailSafeMaxDurationMinutes);
                options.FactorySoftTimeout = TimeSpan.FromMilliseconds(settings.FactorySoftTimeoutMs);
                options.FactoryHardTimeout = TimeSpan.FromMilliseconds(settings.FactoryHardTimeoutMs);
                options.IsFailSafeEnabled = true;
            })
            .WithSystemTextJsonSerializer();

        // OPTIONAL: Add Redis L2 cache for distributed scenarios (multi-replica)
        // Only enable when you need shared cache across app instances.
        // To enable Redis, install: Microsoft.Extensions.Caching.StackExchangeRedis
        // and ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis
        // Then uncomment the following:
        //
        // if (!string.IsNullOrEmpty(settings.RedisConnectionString))
        // {
        //     services.AddStackExchangeRedisCache(options =>
        //     {
        //         options.Configuration = settings.RedisConnectionString;
        //     });
        //
        //     // FusionCache will automatically use IDistributedCache if registered
        //     // For backplane support (cross-replica invalidation), add:
        //     // services.AddFusionCacheStackExchangeRedisBackplane(...)
        // }

        return services;
    }
}
