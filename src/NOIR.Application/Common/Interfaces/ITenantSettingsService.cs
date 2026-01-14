namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for managing tenant settings with platform default fallback.
/// Settings with TenantId = null serve as platform defaults.
/// Tenant-specific settings override platform defaults.
/// </summary>
public interface ITenantSettingsService
{
    /// <summary>
    /// Gets a setting value for a tenant, falling back to platform default if not found.
    /// </summary>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The setting value, or null if not found.</returns>
    Task<string?> GetSettingAsync(string? tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed setting value for a tenant, falling back to platform default if not found.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the setting value to.</typeparam>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed setting value, or default if not found.</returns>
    Task<T?> GetSettingAsync<T>(string? tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all settings with a key prefix for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID, or null for platform defaults only.</param>
    /// <param name="keyPrefix">The key prefix to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of key-value pairs matching the prefix.</returns>
    Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(
        string? tenantId,
        string keyPrefix,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all effective settings for a tenant (tenant-specific merged with platform defaults).
    /// Tenant-specific settings take precedence over platform defaults.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of all effective settings.</returns>
    Task<IReadOnlyDictionary<string, string>> GetEffectiveSettingsAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a setting value for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="dataType">The data type of the setting (e.g., "string", "int", "bool", "json").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSettingAsync(
        string? tenantId,
        string key,
        string value,
        string dataType = "string",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a typed setting value for a tenant.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The typed value to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetSettingAsync<T>(
        string? tenantId,
        string key,
        T value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a setting for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the setting was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteSettingAsync(string? tenantId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a setting exists for a tenant (not including platform defaults).
    /// </summary>
    /// <param name="tenantId">The tenant ID, or null for platform default.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the setting exists for the specific tenant.</returns>
    Task<bool> SettingExistsAsync(string? tenantId, string key, CancellationToken cancellationToken = default);
}
