namespace NOIR.Domain.Enums;

/// <summary>
/// Supported shipping provider codes for Vietnam market.
/// </summary>
public enum ShippingProviderCode
{
    /// <summary>
    /// Giao Hàng Tiết Kiệm - Most popular, fastest delivery (40 hrs avg).
    /// </summary>
    GHTK = 0,

    /// <summary>
    /// Giao Hàng Nhanh - Second largest, excellent API.
    /// </summary>
    GHN = 1,

    /// <summary>
    /// J&amp;T Express Vietnam - 100% on-time rate, fresh product support.
    /// </summary>
    JTExpress = 2,

    /// <summary>
    /// Viettel Post - State-owned, strong rural coverage.
    /// </summary>
    ViettelPost = 3,

    /// <summary>
    /// Ninja Van Vietnam - Tech-focused, returns management.
    /// </summary>
    NinjaVan = 4,

    /// <summary>
    /// Vietnam Post (VNPost) - National postal service, widest coverage.
    /// </summary>
    VNPost = 5,

    /// <summary>
    /// Best Express Vietnam - Budget-friendly option.
    /// </summary>
    BestExpress = 6,

    /// <summary>
    /// Custom/Other provider (for future extensions).
    /// </summary>
    Custom = 99
}
