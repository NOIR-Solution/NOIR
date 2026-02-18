namespace NOIR.Domain.Enums;

/// <summary>
/// Level at which the promotion applies.
/// </summary>
public enum PromotionApplyLevel
{
    /// <summary>
    /// Applies to the entire cart/order.
    /// </summary>
    Cart = 0,

    /// <summary>
    /// Applies to specific products only.
    /// </summary>
    Product = 1,

    /// <summary>
    /// Applies to all products in specific categories.
    /// </summary>
    Category = 2
}
