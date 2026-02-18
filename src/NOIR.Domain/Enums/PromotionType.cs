namespace NOIR.Domain.Enums;

/// <summary>
/// Type of promotion campaign.
/// </summary>
public enum PromotionType
{
    /// <summary>
    /// Voucher code that customers enter at checkout.
    /// </summary>
    VoucherCode = 0,

    /// <summary>
    /// Time-limited flash sale with deep discounts.
    /// </summary>
    FlashSale = 1,

    /// <summary>
    /// Bundle deal (buy multiple items together).
    /// </summary>
    BundleDeal = 2,

    /// <summary>
    /// Free shipping promotion.
    /// </summary>
    FreeShipping = 3
}
