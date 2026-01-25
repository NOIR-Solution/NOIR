namespace NOIR.Domain.Enums;

/// <summary>
/// Status of an order through its lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created, awaiting payment confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment received, order confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Order is being prepared for shipment.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Order has been shipped.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// Order has been delivered to customer.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// Order completed successfully.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Order was cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Order was refunded.
    /// </summary>
    Refunded = 7
}
