namespace NOIR.Domain.Enums;

/// <summary>
/// Status of an inventory reservation.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Temporary hold during checkout (15 min expiry).
    /// </summary>
    Temporary = 0,

    /// <summary>
    /// Confirmed when order is placed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Released when checkout expired or abandoned.
    /// </summary>
    Released = 2,

    /// <summary>
    /// Cancelled when order is cancelled.
    /// </summary>
    Cancelled = 3
}
