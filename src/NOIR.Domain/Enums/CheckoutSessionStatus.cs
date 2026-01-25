namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a checkout session.
/// </summary>
public enum CheckoutSessionStatus
{
    /// <summary>
    /// Checkout session started, collecting information.
    /// </summary>
    Started = 0,

    /// <summary>
    /// Shipping address has been entered.
    /// </summary>
    AddressComplete = 1,

    /// <summary>
    /// Shipping method has been selected.
    /// </summary>
    ShippingSelected = 2,

    /// <summary>
    /// Awaiting payment information.
    /// </summary>
    PaymentPending = 3,

    /// <summary>
    /// Payment is being processed.
    /// </summary>
    PaymentProcessing = 4,

    /// <summary>
    /// Checkout completed, order created.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Checkout session expired (15 minute timeout).
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Checkout was abandoned by user.
    /// </summary>
    Abandoned = 7
}
