namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a tenant in the multi-tenant system.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// Inherits from TenantInfo record for Finbuckle EFCoreStore compatibility.
/// </summary>
/// <remarks>
/// TenantInfo base properties (Id, Identifier, Name) are immutable per C# record semantics.
/// For updates, use the record 'with' expression to create a modified copy.
/// EF Core will track these changes appropriately.
/// </remarks>
public record Tenant : TenantInfo, IAuditableEntity
{
    /// <summary>
    /// Private constructor for EF Core materialization.
    /// Uses dummy values that EF Core will overwrite during materialization.
    /// </summary>
    private Tenant() : base("", "") { }

    /// <summary>
    /// Primary constructor for creating Tenant instances.
    /// </summary>
    public Tenant(string id, string identifier, string? name = null)
        : base(id, identifier, name)
    {
    }

    #region Branding Properties

    /// <summary>
    /// URL to the tenant's logo image.
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// Primary brand color (hex format, e.g., "#3B82F6").
    /// </summary>
    public string? PrimaryColor { get; init; }

    /// <summary>
    /// Accent/secondary brand color (hex format).
    /// </summary>
    public string? AccentColor { get; init; }

    /// <summary>
    /// UI theme preference: "Light", "Dark", or "System".
    /// </summary>
    public string? Theme { get; init; }

    #endregion

    #region Status

    /// <summary>
    /// Whether the tenant is active. Inactive tenants cannot log in.
    /// </summary>
    public bool IsActive { get; init; } = true;

    #endregion

    #region Timestamps (from Entity pattern)

    /// <summary>
    /// Timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    #endregion

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; init; }
    public string? ModifiedBy { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
    public string? DeletedBy { get; init; }

    #endregion

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="identifier">Unique identifier/slug (will be lowercased).</param>
    /// <param name="name">Display name.</param>
    /// <param name="logoUrl">Optional logo URL.</param>
    /// <param name="primaryColor">Optional primary brand color.</param>
    /// <param name="accentColor">Optional accent brand color.</param>
    /// <param name="theme">Optional UI theme preference.</param>
    /// <param name="isActive">Whether the tenant is active (default: true).</param>
    public static Tenant Create(
        string identifier,
        string name,
        string? logoUrl = null,
        string? primaryColor = null,
        string? accentColor = null,
        string? theme = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var id = Guid.NewGuid().ToString();
        return new Tenant(id, identifier.ToLowerInvariant().Trim(), name.Trim())
        {
            LogoUrl = logoUrl?.Trim(),
            PrimaryColor = primaryColor?.Trim(),
            AccentColor = accentColor?.Trim(),
            Theme = theme?.Trim(),
            IsActive = isActive
        };
    }

    /// <summary>
    /// Gets the Id as a Guid for convenience.
    /// </summary>
    public Guid GetGuidId() => Guid.Parse(Id);

    /// <summary>
    /// Creates an updated copy of this tenant with modified properties.
    /// Note: Identifier cannot be changed after creation.
    /// </summary>
    /// <returns>A new Tenant record with the updated values.</returns>
    public Tenant WithUpdatedDetails(
        string name,
        string? logoUrl,
        string? primaryColor,
        string? accentColor,
        string? theme,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return this with
        {
            Name = name.Trim(),
            LogoUrl = logoUrl?.Trim(),
            PrimaryColor = primaryColor?.Trim(),
            AccentColor = accentColor?.Trim(),
            Theme = theme?.Trim(),
            IsActive = isActive,
            ModifiedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates an activated copy of this tenant.
    /// </summary>
    public Tenant WithActivated() => this with { IsActive = true, ModifiedAt = DateTimeOffset.UtcNow };

    /// <summary>
    /// Creates a deactivated copy of this tenant.
    /// </summary>
    public Tenant WithDeactivated() => this with { IsActive = false, ModifiedAt = DateTimeOffset.UtcNow };
}
