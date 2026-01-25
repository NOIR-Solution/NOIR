namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a shopping cart.
/// </summary>
public enum CartStatus
{
    /// <summary>
    /// Cart is active and accepting items.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Cart was abandoned (no activity for 30+ minutes).
    /// </summary>
    Abandoned = 1,

    /// <summary>
    /// Cart was converted to an order.
    /// </summary>
    Converted = 2,

    /// <summary>
    /// Cart expired (session ended).
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Cart was merged into another cart on login.
    /// </summary>
    Merged = 4
}
