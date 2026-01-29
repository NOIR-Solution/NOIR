namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product category with hierarchical support.
/// </summary>
public class ProductCategory : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Category display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Category description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent category ID for hierarchy.
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Display order within parent.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Category image URL.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// SEO meta title.
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description.
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// Cached product count in this category.
    /// </summary>
    public int ProductCount { get; private set; }

    // Navigation properties
    public virtual ProductCategory? Parent { get; private set; }
    public virtual ICollection<ProductCategory> Children { get; private set; } = new List<ProductCategory>();
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();

    // Private constructor for EF Core
    private ProductCategory() : base() { }

    private ProductCategory(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new category.
    /// </summary>
    public static ProductCategory Create(
        string name,
        string slug,
        Guid? parentId = null,
        string? tenantId = null)
    {
        var category = new ProductCategory(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug.ToLowerInvariant(),
            ParentId = parentId,
            SortOrder = 0
        };

        category.AddDomainEvent(new ProductCategoryCreatedEvent(category.Id, name, slug));
        return category;
    }

    /// <summary>
    /// Updates the category details.
    /// </summary>
    public void UpdateDetails(
        string name,
        string slug,
        string? description = null,
        string? imageUrl = null)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        Description = description;
        ImageUrl = imageUrl;

        AddDomainEvent(new ProductCategoryUpdatedEvent(Id, name));
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
    /// Sets the parent category.
    /// </summary>
    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
            throw new InvalidOperationException("Category cannot be its own parent");

        ParentId = parentId;
    }

    /// <summary>
    /// Sets the display order.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
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
        if (ProductCount > 0)
            ProductCount--;
    }

    /// <summary>
    /// Marks the category for deletion (raises ProductCategoryDeletedEvent).
    /// </summary>
    public void MarkAsDeleted()
    {
        AddDomainEvent(new ProductCategoryDeletedEvent(Id));
    }
}
