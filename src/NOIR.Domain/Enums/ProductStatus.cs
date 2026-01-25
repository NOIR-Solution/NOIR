namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a product in the catalog.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is being prepared and not visible.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is published and available for purchase.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Product is archived and hidden from catalog.
    /// </summary>
    Archived = 2,

    /// <summary>
    /// Product is out of stock across all variants.
    /// </summary>
    OutOfStock = 3
}
