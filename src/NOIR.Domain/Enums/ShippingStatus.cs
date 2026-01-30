namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a shipping order throughout its lifecycle.
/// </summary>
public enum ShippingStatus
{
    /// <summary>
    /// Order created but not yet submitted to provider.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Submitted to provider, awaiting pickup.
    /// </summary>
    AwaitingPickup = 1,

    /// <summary>
    /// Package picked up from sender.
    /// </summary>
    PickedUp = 2,

    /// <summary>
    /// Package in transit to destination.
    /// </summary>
    InTransit = 3,

    /// <summary>
    /// Package out for delivery to recipient.
    /// </summary>
    OutForDelivery = 4,

    /// <summary>
    /// Package delivered successfully.
    /// </summary>
    Delivered = 5,

    /// <summary>
    /// Delivery failed (recipient unavailable, wrong address, etc.).
    /// </summary>
    DeliveryFailed = 6,

    /// <summary>
    /// Order cancelled before delivery.
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// Package being returned to sender.
    /// </summary>
    Returning = 8,

    /// <summary>
    /// Package returned to sender.
    /// </summary>
    Returned = 9
}
