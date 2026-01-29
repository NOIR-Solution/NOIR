namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Brand entity for product organization and brand pages.
/// </summary>
public class Brand : TenantAggregateRoot<Guid>
{
    // Identity
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    // Branding
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Website { get; private set; }

    // SEO
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Organization
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }

    // Cached metrics
    public int ProductCount { get; private set; }

    // Navigation
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();

    // Private constructor for EF Core
    private Brand() : base() { }

    private Brand(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new brand.
    /// </summary>
    public static Brand Create(string name, string slug, string? tenantId = null)
    {
        var brand = new Brand(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug.ToLowerInvariant()
        };
        brand.AddDomainEvent(new BrandCreatedEvent(brand.Id, name, slug));
        return brand;
    }

    /// <summary>
    /// Updates basic brand details.
    /// </summary>
    public void UpdateDetails(string name, string slug, string? description, string? website)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        Description = description;
        Website = website;
        AddDomainEvent(new BrandUpdatedEvent(Id, name));
    }

    /// <summary>
    /// Updates branding assets (logo and banner).
    /// </summary>
    public void UpdateBranding(string? logoUrl, string? bannerUrl)
    {
        LogoUrl = logoUrl;
        BannerUrl = bannerUrl;
    }

    /// <summary>
    /// Updates SEO metadata.
    /// </summary>
    public void UpdateSeo(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    /// <summary>
    /// Sets the featured status.
    /// </summary>
    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Updates the cached product count.
    /// </summary>
    public void UpdateProductCount(int count)
    {
        ProductCount = count;
    }

    /// <summary>
    /// Increments the product count.
    /// </summary>
    public void IncrementProductCount()
    {
        ProductCount++;
    }

    /// <summary>
    /// Decrements the product count.
    /// </summary>
    public void DecrementProductCount()
    {
        if (ProductCount > 0) ProductCount--;
    }

    /// <summary>
    /// Sets the display order.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Marks the brand for deletion (raises BrandDeletedEvent).
    /// </summary>
    public void MarkAsDeleted()
    {
        AddDomainEvent(new BrandDeletedEvent(Id));
    }
}
