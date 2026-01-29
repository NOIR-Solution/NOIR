namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Predefined value for Select/MultiSelect attribute types.
/// Supports visual display with color swatches and icons.
/// </summary>
public class ProductAttributeValue : TenantEntity<Guid>
{
    // Identity
    public Guid AttributeId { get; private set; }
    public string Value { get; private set; } = string.Empty;        // "red", "64gb"
    public string DisplayValue { get; private set; } = string.Empty; // "Red", "64 GB"

    // Visual display (for color/size swatches)
    public string? ColorCode { get; private set; }                   // "#FF0000"
    public string? SwatchUrl { get; private set; }                   // Image for pattern/texture
    public string? IconUrl { get; private set; }                     // Icon for the value

    // Organization
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Analytics (denormalized for performance)
    public int ProductCount { get; private set; }

    // Navigation
    public virtual ProductAttribute Attribute { get; private set; } = null!;

    // Private constructor for EF Core
    private ProductAttributeValue() : base() { }

    private ProductAttributeValue(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new attribute value.
    /// </summary>
    public static ProductAttributeValue Create(
        Guid attributeId,
        string value,
        string displayValue,
        int sortOrder = 0,
        string? tenantId = null)
    {
        return new ProductAttributeValue(Guid.NewGuid(), tenantId)
        {
            AttributeId = attributeId,
            Value = value.ToLowerInvariant().Replace(" ", "_"),
            DisplayValue = displayValue,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Updates the value and display value.
    /// </summary>
    public void UpdateValue(string value, string displayValue)
    {
        Value = value.ToLowerInvariant().Replace(" ", "_");
        DisplayValue = displayValue;
    }

    /// <summary>
    /// Sets the visual display properties for swatches.
    /// </summary>
    public void SetVisualDisplay(string? colorCode, string? swatchUrl, string? iconUrl)
    {
        ColorCode = colorCode;
        SwatchUrl = swatchUrl;
        IconUrl = iconUrl;
    }

    /// <summary>
    /// Sets the sort order for display.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Updates the product count (called by domain service).
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
        if (ProductCount > 0)
        {
            ProductCount--;
        }
    }
}
