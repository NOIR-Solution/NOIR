namespace NOIR.Infrastructure.Services;

/// <summary>
/// Implementation of ITenantSettingsService for managing tenant settings.
/// Uses the TenantSetting entity with platform defaults (TenantId = null) fallback pattern.
/// </summary>
public class TenantSettingsService : ITenantSettingsService, IScopedService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantSettingsService> _logger;

    public TenantSettingsService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<TenantSettingsService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string? tenantId, string key, CancellationToken cancellationToken = default)
    {
        // Try tenant-specific first (if tenantId provided)
        if (!string.IsNullOrEmpty(tenantId))
        {
            var tenantSetting = await GetSettingEntityAsync(tenantId, key, cancellationToken);
            if (tenantSetting != null)
                return tenantSetting.Value;
        }

        // Fall back to platform default (TenantId = null)
        var defaultSetting = await GetSettingEntityAsync(null, key, cancellationToken);
        return defaultSetting?.Value;
    }

    public async Task<T?> GetSettingAsync<T>(string? tenantId, string key, CancellationToken cancellationToken = default)
    {
        var value = await GetSettingAsync(tenantId, key, cancellationToken);
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize setting {Key} to type {Type}", key, typeof(T).Name);
            return default;
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(
        string? tenantId,
        string keyPrefix,
        CancellationToken cancellationToken = default)
    {
        var spec = new TenantSettingsByPrefixSpec(tenantId, keyPrefix);
        var settings = await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, spec)
            .ToListAsync(cancellationToken);

        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetEffectiveSettingsAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // Get platform defaults
        var defaultSpec = new TenantSettingsByTenantSpec(null);
        var defaults = await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, defaultSpec)
            .ToListAsync(cancellationToken);

        // Get tenant-specific settings
        var tenantSpec = new TenantSettingsByTenantSpec(tenantId);
        var tenantSettings = await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, tenantSpec)
            .ToListAsync(cancellationToken);

        // Merge: tenant-specific overrides platform defaults
        var result = defaults.ToDictionary(s => s.Key, s => s.Value);
        foreach (var setting in tenantSettings)
        {
            result[setting.Key] = setting.Value;
        }

        return result;
    }

    public async Task SetSettingAsync(
        string? tenantId,
        string key,
        string value,
        string dataType = "string",
        CancellationToken cancellationToken = default)
    {
        var existing = await GetSettingEntityForUpdateAsync(tenantId, key, cancellationToken);

        if (existing != null)
        {
            existing.UpdateValue(value);
        }
        else
        {
            TenantSetting setting;
            if (string.IsNullOrEmpty(tenantId))
            {
                setting = TenantSetting.CreatePlatformDefault(key, value, dataType);
            }
            else
            {
                setting = TenantSetting.CreateTenantOverride(tenantId, key, value, dataType);
            }
            _context.TenantSettings.Add(setting);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Set tenant setting {Key} for tenant {TenantId}",
            key,
            tenantId ?? "platform-default");
    }

    public async Task SetSettingAsync<T>(
        string? tenantId,
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(value);
        var dataType = typeof(T).Name.ToLowerInvariant();
        await SetSettingAsync(tenantId, key, serialized, dataType, cancellationToken);
    }

    public async Task<bool> DeleteSettingAsync(string? tenantId, string key, CancellationToken cancellationToken = default)
    {
        var existing = await GetSettingEntityForUpdateAsync(tenantId, key, cancellationToken);
        if (existing == null)
            return false;

        _context.TenantSettings.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted tenant setting {Key} for tenant {TenantId}",
            key,
            tenantId ?? "platform-default");

        return true;
    }

    public async Task<bool> SettingExistsAsync(string? tenantId, string key, CancellationToken cancellationToken = default)
    {
        var spec = new TenantSettingByKeySpec(tenantId, key);
        return await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, spec)
            .AnyAsync(cancellationToken);
    }

    private async Task<TenantSetting?> GetSettingEntityAsync(string? tenantId, string key, CancellationToken cancellationToken)
    {
        var spec = new TenantSettingByKeySpec(tenantId, key);
        return await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, spec)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TenantSetting?> GetSettingEntityForUpdateAsync(string? tenantId, string key, CancellationToken cancellationToken)
    {
        var spec = new TenantSettingByKeySpec(tenantId, key, asTracking: true);
        return await SpecificationEvaluator
            .GetQuery(_context.TenantSettings, spec)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
