namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Denormalized table for high-performance filtering.
/// Updated via domain events when products change.
/// </summary>
public class ProductFilterIndex : TenantEntity<Guid>
{
    /// <summary>
    /// Reference to the source product (1:1 relationship).
    /// </summary>
    public Guid ProductId { get; private set; }

    #region Denormalized Product Info

    /// <summary>
    /// Product name for display and search.
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// Product slug for URLs.
    /// </summary>
    public string ProductSlug { get; private set; } = string.Empty;

    /// <summary>
    /// Product status for availability filtering.
    /// </summary>
    public ProductStatus Status { get; private set; }

    /// <summary>
    /// Product SKU for reference.
    /// </summary>
    public string? Sku { get; private set; }

    #endregion

    #region Category (Denormalized for Hierarchy Filtering)

    /// <summary>
    /// Product's category ID.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Materialized path for hierarchy queries (e.g., "1/5/23").
    /// Enables efficient "all products in category and descendants" queries.
    /// </summary>
    public string? CategoryPath { get; private set; }

    /// <summary>
    /// Category name for display.
    /// </summary>
    public string? CategoryName { get; private set; }

    /// <summary>
    /// Category slug for URLs.
    /// </summary>
    public string? CategorySlug { get; private set; }

    #endregion

    #region Brand (Denormalized)

    /// <summary>
    /// Brand ID for filtering.
    /// </summary>
    public Guid? BrandId { get; private set; }

    /// <summary>
    /// Brand name for display.
    /// </summary>
    public string? BrandName { get; private set; }

    /// <summary>
    /// Brand slug for URLs.
    /// </summary>
    public string? BrandSlug { get; private set; }

    #endregion

    #region Pricing (Aggregated from Variants)

    /// <summary>
    /// Minimum price across all variants.
    /// </summary>
    public decimal MinPrice { get; private set; }

    /// <summary>
    /// Maximum price across all variants.
    /// </summary>
    public decimal MaxPrice { get; private set; }

    /// <summary>
    /// Product currency.
    /// </summary>
    public string Currency { get; private set; } = "VND";

    #endregion

    #region Inventory

    /// <summary>
    /// Whether product is in stock (any variant has stock).
    /// </summary>
    public bool InStock { get; private set; }

    /// <summary>
    /// Total stock across all variants.
    /// </summary>
    public int TotalStock { get; private set; }

    #endregion

    #region Reviews (Future)

    /// <summary>
    /// Average rating for rating-based filtering.
    /// </summary>
    public decimal? AverageRating { get; private set; }

    /// <summary>
    /// Total review count.
    /// </summary>
    public int ReviewCount { get; private set; }

    #endregion

    #region Flexible Attribute Filtering

    /// <summary>
    /// JSONB storage for attribute values.
    /// Format: {"color": ["red", "blue"], "size": ["m", "l"], "screen_size": 6.7}
    /// Enables flexible filtering without schema changes.
    /// </summary>
    public string AttributesJson { get; private set; } = "{}";

    #endregion

    #region Search

    /// <summary>
    /// Concatenated text for full-text search.
    /// Includes: name, description, brand, category, SKU, attribute values.
    /// </summary>
    public string SearchText { get; private set; } = string.Empty;

    #endregion

    #region Display

    /// <summary>
    /// Primary image URL for list display.
    /// </summary>
    public string? PrimaryImageUrl { get; private set; }

    /// <summary>
    /// Product sort order.
    /// </summary>
    public int SortOrder { get; private set; }

    #endregion

    #region Timestamps

    /// <summary>
    /// When this index was last synchronized.
    /// </summary>
    public DateTime LastSyncedAt { get; private set; }

    /// <summary>
    /// When the source product was last updated.
    /// Used for stale detection.
    /// </summary>
    public DateTime ProductUpdatedAt { get; private set; }

    #endregion

    #region Navigation

    /// <summary>
    /// Reference to the source product.
    /// </summary>
    public virtual Product? Product { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private ProductFilterIndex() : base() { }

    private ProductFilterIndex(Guid id, string? tenantId) : base(id, tenantId) { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new filter index entry for a product.
    /// </summary>
    public static ProductFilterIndex Create(
        Guid productId,
        string productName,
        string productSlug,
        ProductStatus status,
        decimal basePrice,
        string currency,
        string? tenantId = null)
    {
        return new ProductFilterIndex(Guid.NewGuid(), tenantId)
        {
            ProductId = productId,
            ProductName = productName,
            ProductSlug = productSlug,
            Status = status,
            MinPrice = basePrice,
            MaxPrice = basePrice,
            Currency = currency,
            LastSyncedAt = DateTime.UtcNow,
            ProductUpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates the index from a product with all related entities loaded.
    /// </summary>
    public void UpdateFromProduct(
        Product product,
        ProductCategory? category,
        Brand? brand,
        string? categoryPath = null)
    {
        // Basic info
        ProductName = product.Name;
        ProductSlug = product.Slug;
        Status = product.Status;
        Sku = product.Sku;
        SortOrder = product.SortOrder;

        // Category
        CategoryId = product.CategoryId;
        CategoryPath = categoryPath;
        CategoryName = category?.Name;
        CategorySlug = category?.Slug;

        // Brand
        BrandId = product.BrandId;
        BrandName = brand?.Name ?? product.Brand;
        BrandSlug = brand?.Slug;

        // Pricing from variants
        if (product.Variants.Any())
        {
            MinPrice = product.Variants.Min(v => v.Price);
            MaxPrice = product.Variants.Max(v => v.Price);
        }
        else
        {
            MinPrice = product.BasePrice;
            MaxPrice = product.BasePrice;
        }
        Currency = product.Currency;

        // Inventory
        TotalStock = product.TotalStock;
        InStock = product.InStock;

        // Primary image
        PrimaryImageUrl = product.PrimaryImage?.Url;

        // Search text (will be enhanced with attribute values)
        UpdateSearchText(product, brand, category);

        // Timestamps
        LastSyncedAt = DateTime.UtcNow;
        ProductUpdatedAt = (product.ModifiedAt ?? product.CreatedAt).UtcDateTime;
    }

    /// <summary>
    /// Updates the attributes JSON for filtering.
    /// </summary>
    public void SetAttributesJson(string attributesJson)
    {
        AttributesJson = attributesJson ?? "{}";
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the search text vector.
    /// </summary>
    public void SetSearchText(string searchText)
    {
        SearchText = searchText ?? string.Empty;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates category path for hierarchy queries.
    /// </summary>
    public void SetCategoryPath(string? categoryPath)
    {
        CategoryPath = categoryPath;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates stock information.
    /// </summary>
    public void UpdateStock(int totalStock, bool inStock)
    {
        TotalStock = totalStock;
        InStock = inStock;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates pricing range.
    /// </summary>
    public void UpdatePricing(decimal minPrice, decimal maxPrice, string currency)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Currency = currency;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates rating information.
    /// </summary>
    public void UpdateRating(decimal? averageRating, int reviewCount)
    {
        AverageRating = averageRating;
        ReviewCount = reviewCount;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the index as stale (needs re-sync).
    /// </summary>
    public void MarkAsStale()
    {
        // Set timestamp in the past to trigger re-sync
        LastSyncedAt = DateTime.MinValue;
    }

    #endregion

    #region Private Methods

    private void UpdateSearchText(Product product, Brand? brand, ProductCategory? category)
    {
        var searchParts = new List<string>
        {
            product.Name,
            product.Slug,
            product.Sku ?? string.Empty,
            product.ShortDescription ?? string.Empty,
            brand?.Name ?? product.Brand ?? string.Empty,
            category?.Name ?? string.Empty
        };

        // Add variant names and SKUs
        foreach (var variant in product.Variants)
        {
            searchParts.Add(variant.Name);
            if (!string.IsNullOrEmpty(variant.Sku))
                searchParts.Add(variant.Sku);
        }

        SearchText = string.Join(" ", searchParts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    #endregion
}
