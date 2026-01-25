namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product image for gallery.
/// </summary>
public class ProductImage : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;

    // Private constructor for EF Core
    private ProductImage() { }

    /// <summary>
    /// Factory method to create a new product image.
    /// </summary>
    internal static ProductImage Create(
        Guid productId,
        string url,
        string? altText,
        int sortOrder,
        bool isPrimary,
        string? tenantId)
    {
        return new ProductImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Url = url,
            AltText = altText,
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }

    /// <summary>
    /// Updates the image details.
    /// </summary>
    public void Update(string url, string? altText)
    {
        Url = url;
        AltText = altText;
    }

    /// <summary>
    /// Sets the sort order.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Sets this image as the primary image.
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Clears the primary flag.
    /// </summary>
    public void ClearPrimary()
    {
        IsPrimary = false;
    }
}
