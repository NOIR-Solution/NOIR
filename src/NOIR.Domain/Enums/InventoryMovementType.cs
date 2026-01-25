namespace NOIR.Domain.Enums;

/// <summary>
/// Type of inventory movement for audit trail.
/// </summary>
public enum InventoryMovementType
{
    /// <summary>
    /// Stock received from supplier.
    /// </summary>
    StockIn = 0,

    /// <summary>
    /// Stock removed for order fulfillment.
    /// </summary>
    StockOut = 1,

    /// <summary>
    /// Manual inventory adjustment.
    /// </summary>
    Adjustment = 2,

    /// <summary>
    /// Stock returned from customer.
    /// </summary>
    Return = 3,

    /// <summary>
    /// Stock reserved during checkout.
    /// </summary>
    Reservation = 4,

    /// <summary>
    /// Reserved stock released back to inventory.
    /// </summary>
    ReservationRelease = 5,

    /// <summary>
    /// Stock marked as damaged.
    /// </summary>
    Damaged = 6,

    /// <summary>
    /// Stock marked as expired.
    /// </summary>
    Expired = 7
}
