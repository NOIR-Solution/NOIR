namespace NOIR.Domain.Enums;

/// <summary>
/// How the discount is calculated.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Fixed amount off (e.g., 50,000 VND off).
    /// </summary>
    FixedAmount = 0,

    /// <summary>
    /// Percentage off (e.g., 20% off).
    /// </summary>
    Percentage = 1,

    /// <summary>
    /// Free shipping discount.
    /// </summary>
    FreeShipping = 2,

    /// <summary>
    /// Buy X get Y free.
    /// </summary>
    BuyXGetY = 3
}
