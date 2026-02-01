namespace NOIR.Domain.Entities;

/// <summary>
/// Legal page entity for storing customizable legal content (Terms of Service, Privacy Policy, etc.).
/// Platform-level entity - NOT scoped to any tenant by default.
/// When TenantId is null, this is a platform-level default page.
/// When TenantId has a value, this is a tenant-specific override (copy-on-write).
/// </summary>
public class LegalPage : PlatformTenantAggregateRoot<Guid>, ISeedableEntity
{
    /// <summary>
    /// URL-friendly identifier (e.g., "terms-of-service", "privacy-policy").
    /// Used to look up pages programmatically and for public URLs.
    /// </summary>
    public string Slug { get; private set; } = default!;

    /// <summary>
    /// Display title (e.g., "Terms of Service", "Privacy Policy").
    /// </summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// Rich HTML content of the legal page.
    /// </summary>
    public string HtmlContent { get; private set; } = default!;

    /// <summary>
    /// SEO meta title for the page (max 60 characters recommended).
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description for the page (max 160 characters recommended).
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// Canonical URL for the page. Leave null to use default URL.
    /// </summary>
    public string? CanonicalUrl { get; private set; }

    /// <summary>
    /// Whether search engines should index this page.
    /// </summary>
    public bool AllowIndexing { get; private set; } = true;

    /// <summary>
    /// Whether this page is active and should be publicly accessible.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Page version for tracking changes.
    /// Incremented each time the page is updated.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// When the content was last modified.
    /// </summary>
    public DateTimeOffset LastModified { get; private set; }

    // Private constructor for EF Core
    private LegalPage() : base() { }

    /// <summary>
    /// Creates a platform-level default legal page (TenantId = null).
    /// Platform pages are shared across all tenants and serve as defaults.
    /// </summary>
    public static LegalPage CreatePlatformDefault(
        string slug,
        string title,
        string htmlContent,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool allowIndexing = true)
    {
        var page = new LegalPage
        {
            Id = Guid.NewGuid(),
            TenantId = null, // Platform default
            Slug = slug,
            Title = title,
            HtmlContent = htmlContent,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            CanonicalUrl = canonicalUrl,
            AllowIndexing = allowIndexing,
            IsActive = true,
            Version = 1,
            LastModified = DateTimeOffset.UtcNow
        };

        page.AddDomainEvent(new Events.Platform.LegalPageCreatedEvent(
            page.Id,
            slug,
            null));

        return page;
    }

    /// <summary>
    /// Creates a tenant-specific legal page override.
    /// Used when a tenant customizes a platform legal page (copy-on-write pattern).
    /// </summary>
    public static LegalPage CreateTenantOverride(
        string tenantId,
        string slug,
        string title,
        string htmlContent,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool allowIndexing = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

        var page = new LegalPage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Slug = slug,
            Title = title,
            HtmlContent = htmlContent,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            CanonicalUrl = canonicalUrl,
            AllowIndexing = allowIndexing,
            IsActive = true,
            Version = 1,
            LastModified = DateTimeOffset.UtcNow
        };

        page.AddDomainEvent(new Events.Platform.LegalPageCreatedEvent(
            page.Id,
            slug,
            tenantId));

        return page;
    }

    /// <summary>
    /// Updates the page content and increments version.
    /// </summary>
    public void Update(
        string title,
        string htmlContent,
        string? metaTitle = null,
        string? metaDescription = null,
        string? canonicalUrl = null,
        bool allowIndexing = true)
    {
        Title = title;
        HtmlContent = htmlContent;
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        CanonicalUrl = canonicalUrl;
        AllowIndexing = allowIndexing;
        Version++;
        LastModified = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Platform.LegalPageUpdatedEvent(Id, Slug, Version));
    }

    /// <summary>
    /// Activates the page for public access.
    /// </summary>
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            AddDomainEvent(new Events.Platform.LegalPageActivatedEvent(Id, Slug));
        }
    }

    /// <summary>
    /// Deactivates the page from public access.
    /// </summary>
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            AddDomainEvent(new Events.Platform.LegalPageDeactivatedEvent(Id, Slug));
        }
    }

    /// <summary>
    /// Resets version to 1. FOR SEEDING USE ONLY.
    /// This is used when platform defaults are updated during application startup.
    /// Should NOT be called from business logic - only from database seeders.
    /// </summary>
    public void ResetVersionForSeeding()
    {
        Version = 1;
    }
}
