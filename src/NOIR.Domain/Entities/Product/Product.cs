namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product in the catalog.
/// </summary>
public class Product : TenantAggregateRoot<Guid>
{
    // Basic Info
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? Description { get; private set; }
    public string? DescriptionHtml { get; private set; }

    // Pricing
    public decimal BasePrice { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Status
    public ProductStatus Status { get; private set; }

    // Organization
    public Guid? CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string? Brand { get; private set; }

    // Identification
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }

    // Inventory
    public bool TrackInventory { get; private set; } = true;

    // SEO
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Display
    public int SortOrder { get; private set; }

    // Navigation
    public virtual ProductCategory? Category { get; private set; }
    public virtual Brand? BrandEntity { get; private set; }
    public virtual ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();
    public virtual ICollection<ProductOption> Options { get; private set; } = new List<ProductOption>();
    public virtual ICollection<ProductAttributeAssignment> AttributeAssignments { get; private set; } = new List<ProductAttributeAssignment>();

    // Computed
    public bool HasVariants => Variants.Any();
    public bool HasOptions => Options.Any();
    public int TotalStock => Variants.Sum(v => v.StockQuantity);
    public bool InStock => TotalStock > 0;
    public ProductImage? PrimaryImage => Images.FirstOrDefault(i => i.IsPrimary) ?? Images.FirstOrDefault();

    // Private constructor for EF Core
    private Product() : base() { }

    private Product(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new product.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static Product Create(
        string name,
        string slug,
        decimal basePrice,
        string currency = "VND",
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        if (basePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price cannot be negative.");

        var product = new Product(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug.ToLowerInvariant(),
            BasePrice = basePrice,
            Currency = currency,
            Status = ProductStatus.Draft,
            TrackInventory = true
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, slug));
        return product;
    }

    /// <summary>
    /// Updates basic product information.
    /// </summary>
    public void UpdateBasicInfo(
        string name,
        string slug,
        string? shortDescription,
        string? description,
        string? descriptionHtml)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        ShortDescription = shortDescription?.Trim();
        Description = description;
        DescriptionHtml = descriptionHtml;
        AddDomainEvent(new ProductUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Updates product pricing.
    /// </summary>
    public void UpdatePricing(decimal basePrice, string currency = "VND")
    {
        BasePrice = basePrice;
        Currency = currency;
        AddDomainEvent(new ProductUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Sets the product category.
    /// </summary>
    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
        AddDomainEvent(new ProductUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Sets the product brand (legacy string field).
    /// </summary>
    public void SetBrand(string? brand)
    {
        Brand = brand;
    }

    /// <summary>
    /// Sets the product brand by ID (new Brand entity reference).
    /// </summary>
    public void SetBrandId(Guid? brandId)
    {
        BrandId = brandId;
        AddDomainEvent(new ProductUpdatedEvent(Id, Name));
    }

    /// <summary>
    /// Updates product identification codes.
    /// </summary>
    public void UpdateIdentification(string? sku, string? barcode)
    {
        Sku = sku;
        Barcode = barcode;
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
    /// Sets whether to track inventory for this product.
    /// </summary>
    public void SetInventoryTracking(bool trackInventory)
    {
        TrackInventory = trackInventory;
    }

    /// <summary>
    /// Publishes the product (makes it available for purchase).
    /// </summary>
    public void Publish()
    {
        if (Status == ProductStatus.Draft)
        {
            Status = ProductStatus.Active;
            AddDomainEvent(new ProductPublishedEvent(Id, Name));
        }
    }

    /// <summary>
    /// Archives the product (hides from catalog).
    /// </summary>
    public void Archive()
    {
        Status = ProductStatus.Archived;
        AddDomainEvent(new ProductArchivedEvent(Id));
    }

    /// <summary>
    /// Marks the product as out of stock.
    /// </summary>
    public void SetOutOfStock()
    {
        if (TotalStock == 0)
        {
            Status = ProductStatus.OutOfStock;
        }
    }

    /// <summary>
    /// Restores product from out of stock when inventory is added.
    /// </summary>
    public void RestoreFromOutOfStock()
    {
        if (Status == ProductStatus.OutOfStock && TotalStock > 0)
        {
            Status = ProductStatus.Active;
        }
    }

    /// <summary>
    /// Adds a new variant to the product.
    /// </summary>
    public ProductVariant AddVariant(
        string name,
        decimal price,
        string? sku = null,
        Dictionary<string, string>? options = null)
    {
        var variant = ProductVariant.Create(Id, name, price, sku, options, TenantId);
        Variants.Add(variant);
        return variant;
    }

    /// <summary>
    /// Removes a variant from the product.
    /// </summary>
    public void RemoveVariant(Guid variantId)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId);
        if (variant != null)
        {
            Variants.Remove(variant);
        }
    }

    /// <summary>
    /// Adds an image to the product gallery.
    /// </summary>
    public ProductImage AddImage(string url, string? altText = null, bool isPrimary = false)
    {
        // If setting as primary, clear other primaries
        if (isPrimary)
        {
            foreach (var img in Images.Where(i => i.IsPrimary))
            {
                img.ClearPrimary();
            }
        }

        var sortOrder = Images.Any() ? Images.Max(i => i.SortOrder) + 1 : 0;
        var image = ProductImage.Create(Id, url, altText, sortOrder, isPrimary, TenantId);
        Images.Add(image);
        return image;
    }

    /// <summary>
    /// Removes an image from the product gallery.
    /// </summary>
    public void RemoveImage(Guid imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            Images.Remove(image);
        }
    }

    /// <summary>
    /// Sets the primary image for the product.
    /// </summary>
    public void SetPrimaryImage(Guid imageId)
    {
        foreach (var img in Images)
        {
            if (img.Id == imageId)
                img.SetAsPrimary();
            else
                img.ClearPrimary();
        }
    }

    /// <summary>
    /// Adds a new option to the product (e.g., "Color", "Size").
    /// </summary>
    public ProductOption AddOption(string name, string? displayName = null)
    {
        var sortOrder = Options.Any() ? Options.Max(o => o.SortOrder) + 1 : 0;
        var option = ProductOption.Create(Id, name, displayName, sortOrder, TenantId);
        Options.Add(option);
        return option;
    }

    /// <summary>
    /// Removes an option from the product.
    /// </summary>
    public void RemoveOption(Guid optionId)
    {
        var option = Options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            Options.Remove(option);
        }
    }
}
