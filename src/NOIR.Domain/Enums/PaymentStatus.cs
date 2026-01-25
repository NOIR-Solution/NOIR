namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment created, awaiting action.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment sent to gateway for processing.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payment requires additional action (3DS, QR scan, etc.).
    /// </summary>
    RequiresAction = 2,

    /// <summary>
    /// Payment authorized but not yet captured.
    /// </summary>
    Authorized = 3,

    /// <summary>
    /// Payment successfully completed.
    /// </summary>
    Paid = 4,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Payment cancelled by user.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Payment link has expired.
    /// </summary>
    Expired = 7,

    /// <summary>
    /// Payment fully refunded.
    /// </summary>
    Refunded = 8,

    /// <summary>
    /// Payment partially refunded.
    /// </summary>
    PartialRefund = 9,

    /// <summary>
    /// COD payment awaiting delivery collection.
    /// </summary>
    CodPending = 10,

    /// <summary>
    /// COD cash collected by courier.
    /// </summary>
    CodCollected = 11
}
