namespace NOIR.Domain.Enums;

/// <summary>
/// Billing frequency for subscriptions.
/// </summary>
public enum BillingInterval
{
    /// <summary>
    /// Billed daily.
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Billed weekly.
    /// </summary>
    Weekly = 7,

    /// <summary>
    /// Billed monthly.
    /// </summary>
    Monthly = 30,

    /// <summary>
    /// Billed quarterly.
    /// </summary>
    Quarterly = 90,

    /// <summary>
    /// Billed yearly.
    /// </summary>
    Yearly = 365
}
