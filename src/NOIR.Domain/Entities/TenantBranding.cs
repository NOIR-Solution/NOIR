namespace NOIR.Domain.Entities;

/// <summary>
/// Represents the white-label branding configuration for a tenant.
/// Includes logo, colors, and visual customization options.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// One-to-one relationship with Tenant.
/// </summary>
public class TenantBranding : Entity<Guid>, IAuditableEntity
{
    /// <summary>
    /// The tenant this branding belongs to (FK to Tenants).
    /// </summary>
    public string TenantId { get; private set; } = default!;

    #region Logo Settings

    /// <summary>
    /// URL to the tenant's logo for light backgrounds.
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// URL to the tenant's logo for dark backgrounds/dark mode.
    /// </summary>
    public string? LogoDarkUrl { get; private set; }

    /// <summary>
    /// URL to the tenant's favicon.
    /// </summary>
    public string? FaviconUrl { get; private set; }

    #endregion

    #region Color Settings

    /// <summary>
    /// Primary brand color (e.g., "#3B82F6").
    /// Used for primary buttons, links, and accents.
    /// </summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>
    /// Secondary brand color.
    /// Used for secondary UI elements.
    /// </summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>
    /// Accent color for highlights and special elements.
    /// </summary>
    public string? AccentColor { get; private set; }

    /// <summary>
    /// Background color for the application.
    /// </summary>
    public string? BackgroundColor { get; private set; }

    /// <summary>
    /// Text color for primary content.
    /// </summary>
    public string? TextColor { get; private set; }

    #endregion

    #region IAuditableEntity Implementation
    // CreatedAt and ModifiedAt are inherited from Entity<Guid>

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    // Private constructor for EF Core
    private TenantBranding() : base() { }

    /// <summary>
    /// Creates a new branding configuration with default (empty) values.
    /// </summary>
    public static TenantBranding CreateDefault(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

        return new TenantBranding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Creates a new branding configuration with specified values.
    /// </summary>
    public static TenantBranding Create(
        string tenantId,
        string? logoUrl = null,
        string? logoDarkUrl = null,
        string? faviconUrl = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? accentColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

        return new TenantBranding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LogoUrl = logoUrl,
            LogoDarkUrl = logoDarkUrl,
            FaviconUrl = faviconUrl,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            AccentColor = accentColor
        };
    }

    /// <summary>
    /// Updates the logo URLs.
    /// </summary>
    public void UpdateLogos(string? logoUrl, string? logoDarkUrl, string? faviconUrl)
    {
        LogoUrl = logoUrl;
        LogoDarkUrl = logoDarkUrl;
        FaviconUrl = faviconUrl;
    }

    /// <summary>
    /// Updates the color scheme.
    /// </summary>
    public void UpdateColors(
        string? primaryColor,
        string? secondaryColor,
        string? accentColor,
        string? backgroundColor = null,
        string? textColor = null)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        AccentColor = accentColor;
        BackgroundColor = backgroundColor;
        TextColor = textColor;
    }

    /// <summary>
    /// Resets all branding to default (null values).
    /// </summary>
    public void ResetToDefault()
    {
        LogoUrl = null;
        LogoDarkUrl = null;
        FaviconUrl = null;
        PrimaryColor = null;
        SecondaryColor = null;
        AccentColor = null;
        BackgroundColor = null;
        TextColor = null;
    }

    /// <summary>
    /// Checks if any branding customization is set.
    /// </summary>
    public bool HasCustomization =>
        !string.IsNullOrEmpty(LogoUrl) ||
        !string.IsNullOrEmpty(LogoDarkUrl) ||
        !string.IsNullOrEmpty(FaviconUrl) ||
        !string.IsNullOrEmpty(PrimaryColor) ||
        !string.IsNullOrEmpty(SecondaryColor) ||
        !string.IsNullOrEmpty(AccentColor);
}
