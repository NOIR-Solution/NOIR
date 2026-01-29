namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Junction entity linking ProductCategory to ProductAttribute.
/// Defines which attributes are available for products in a category.
/// </summary>
public class CategoryAttribute : TenantEntity<Guid>
{
    public Guid CategoryId { get; private set; }
    public Guid AttributeId { get; private set; }

    // Category-specific overrides
    public bool IsRequired { get; private set; }                     // Override attribute default
    public int SortOrder { get; private set; }                       // Display order in category

    // Navigation
    public virtual ProductCategory Category { get; private set; } = null!;
    public virtual ProductAttribute Attribute { get; private set; } = null!;

    // Private constructor for EF Core
    private CategoryAttribute() : base() { }

    private CategoryAttribute(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new category-attribute link.
    /// </summary>
    public static CategoryAttribute Create(
        Guid categoryId,
        Guid attributeId,
        bool isRequired = false,
        int sortOrder = 0,
        string? tenantId = null)
    {
        return new CategoryAttribute(Guid.NewGuid(), tenantId)
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            IsRequired = isRequired,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Sets whether this attribute is required for the category.
    /// </summary>
    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }

    /// <summary>
    /// Sets the display sort order within the category.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
