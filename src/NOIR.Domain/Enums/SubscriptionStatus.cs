namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is in trial period.
    /// </summary>
    Trialing,

    /// <summary>
    /// Subscription is active and billing normally.
    /// </summary>
    Active,

    /// <summary>
    /// Payment failed, subscription is past due.
    /// </summary>
    PastDue,

    /// <summary>
    /// Subscription has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Subscription has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Subscription is temporarily paused.
    /// </summary>
    Paused
}
