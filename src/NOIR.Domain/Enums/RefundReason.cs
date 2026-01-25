namespace NOIR.Domain.Enums;

/// <summary>
/// Reason for refund request.
/// </summary>
public enum RefundReason
{
    /// <summary>
    /// Customer requested refund.
    /// </summary>
    CustomerRequest = 0,

    /// <summary>
    /// Product was defective.
    /// </summary>
    Defective = 1,

    /// <summary>
    /// Wrong item received.
    /// </summary>
    WrongItem = 2,

    /// <summary>
    /// Item not delivered.
    /// </summary>
    NotDelivered = 3,

    /// <summary>
    /// Duplicate payment.
    /// </summary>
    Duplicate = 4,

    /// <summary>
    /// Other reason.
    /// </summary>
    Other = 5
}
