namespace NOIR.Domain.Enums;

/// <summary>
/// Types of shipping services offered by providers.
/// </summary>
public enum ShippingServiceType
{
    /// <summary>
    /// Standard delivery (2-5 business days).
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Express delivery (1-2 business days).
    /// </summary>
    Express = 1,

    /// <summary>
    /// Same-day delivery (within 24 hours).
    /// </summary>
    SameDay = 2,

    /// <summary>
    /// Next-day delivery (guaranteed next business day).
    /// </summary>
    NextDay = 3,

    /// <summary>
    /// Economy delivery (5-7 business days, lowest cost).
    /// </summary>
    Economy = 4,

    /// <summary>
    /// Fresh/perishable goods delivery (temperature controlled).
    /// </summary>
    Fresh = 5,

    /// <summary>
    /// Bulky/heavy items delivery.
    /// </summary>
    Bulky = 6,

    /// <summary>
    /// International shipping.
    /// </summary>
    International = 7
}
