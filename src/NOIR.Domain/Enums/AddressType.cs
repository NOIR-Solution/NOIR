namespace NOIR.Domain.Enums;

/// <summary>
/// Type of customer address.
/// </summary>
public enum AddressType
{
    /// <summary>Shipping address.</summary>
    Shipping = 0,

    /// <summary>Billing address.</summary>
    Billing = 1,

    /// <summary>Used for both shipping and billing.</summary>
    Both = 2
}
