namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a configuration setting with support for platform defaults and tenant overrides.
/// When TenantId is null, this is a platform-level default setting.
/// When TenantId has a value, this is a tenant-specific override.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// </summary>
public class TenantSetting : PlatformTenantEntity<Guid>
{
    /// <summary>
    /// The setting key/name (e.g., "max_users", "default_language").
    /// </summary>
    public string Key { get; private set; } = default!;

    /// <summary>
    /// The setting value (stored as string, parse according to DataType).
    /// </summary>
    public string Value { get; private set; } = default!;

    /// <summary>
    /// Description of what this setting controls.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The data type of the value for parsing/validation.
    /// Common values: "string", "int", "bool", "json", "decimal".
    /// </summary>
    public string DataType { get; private set; } = "string";

    /// <summary>
    /// Optional grouping category for settings (e.g., "email", "security", "ui").
    /// </summary>
    public string? Category { get; private set; }

    // Note: TenantId, IsPlatformDefault, IsTenantOverride, and IAuditableEntity properties
    // are inherited from PlatformTenantEntity<Guid> base class

    // Private constructor for EF Core
    private TenantSetting() : base() { }

    /// <summary>
    /// Creates a platform-level default setting (TenantId = null).
    /// </summary>
    public static TenantSetting CreatePlatformDefault(
        string key,
        string value,
        string dataType = "string",
        string? description = null,
        string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        return new TenantSetting
        {
            Id = Guid.NewGuid(),
            TenantId = null, // Platform default
            Key = key.ToLowerInvariant().Trim(),
            Value = value,
            DataType = dataType,
            Description = description,
            Category = category?.ToLowerInvariant().Trim()
        };
    }

    /// <summary>
    /// Creates a tenant-specific setting override.
    /// </summary>
    public static TenantSetting CreateTenantOverride(
        string tenantId,
        string key,
        string value,
        string dataType = "string",
        string? description = null,
        string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        return new TenantSetting
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = key.ToLowerInvariant().Trim(),
            Value = value,
            DataType = dataType,
            Description = description,
            Category = category?.ToLowerInvariant().Trim()
        };
    }

    /// <summary>
    /// Updates the setting value.
    /// </summary>
    public void UpdateValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        Value = value;
    }

    /// <summary>
    /// Updates the setting metadata.
    /// </summary>
    public void UpdateMetadata(string? description, string? category)
    {
        Description = description;
        Category = category?.ToLowerInvariant().Trim();
    }

    #region Type-Safe Value Accessors

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    public string GetStringValue() => Value;

    /// <summary>
    /// Gets the value as an integer.
    /// </summary>
    public int GetIntValue() => int.Parse(Value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the value as a boolean.
    /// </summary>
    public bool GetBoolValue() => bool.Parse(Value);

    /// <summary>
    /// Gets the value as a decimal.
    /// </summary>
    public decimal GetDecimalValue() => decimal.Parse(Value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Tries to get the value as the specified type.
    /// </summary>
    public bool TryGetValue<T>(out T? result) where T : IParsable<T>
    {
        return T.TryParse(Value, CultureInfo.InvariantCulture, out result);
    }

    #endregion
}
