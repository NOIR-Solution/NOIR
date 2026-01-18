namespace NOIR.Application.Common.Settings;

/// <summary>
/// FusionCache configuration settings.
/// FusionCache provides L1 (in-memory) + optional L2 (distributed) hybrid caching
/// with stampede protection, fail-safe, and soft/hard timeouts.
/// </summary>
public class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Default cache entry expiration in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "DefaultExpirationMinutes must be between 1 and 1440")]
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Permission cache expiration in minutes (longer for less volatile data).
    /// </summary>
    [Range(1, 1440, ErrorMessage = "PermissionExpirationMinutes must be between 1 and 1440")]
    public int PermissionExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// User profile cache expiration in minutes.
    /// </summary>
    [Range(1, 1440, ErrorMessage = "UserProfileExpirationMinutes must be between 1 and 1440")]
    public int UserProfileExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Blog post cache expiration in minutes (shorter for frequently updated content).
    /// </summary>
    [Range(1, 1440, ErrorMessage = "BlogPostExpirationMinutes must be between 1 and 1440")]
    public int BlogPostExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum duration (in minutes) to serve stale data when backend is unavailable (fail-safe).
    /// </summary>
    [Range(1, 1440, ErrorMessage = "FailSafeMaxDurationMinutes must be between 1 and 1440")]
    public int FailSafeMaxDurationMinutes { get; set; } = 120;

    /// <summary>
    /// Soft timeout (in milliseconds) - start returning stale data if factory takes longer than this.
    /// </summary>
    [Range(10, 10000, ErrorMessage = "FactorySoftTimeoutMs must be between 10 and 10000")]
    public int FactorySoftTimeoutMs { get; set; } = 100;

    /// <summary>
    /// Hard timeout (in milliseconds) - absolute maximum wait time for factory.
    /// </summary>
    [Range(100, 60000, ErrorMessage = "FactoryHardTimeoutMs must be between 100 and 60000")]
    public int FactoryHardTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Enable backplane for cache invalidation across multiple app instances.
    /// Requires RedisConnectionString to be set.
    /// </summary>
    public bool EnableBackplane { get; set; } = false;

    /// <summary>
    /// Redis connection string for L2 distributed cache and backplane.
    /// Leave null to use in-memory only (L1 cache).
    /// </summary>
    public string? RedisConnectionString { get; set; }
}
