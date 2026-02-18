namespace NOIR.Domain.Enums;

/// <summary>
/// Status of an inventory receipt (phieu nhap/xuat kho).
/// </summary>
public enum InventoryReceiptStatus
{
    /// <summary>
    /// Receipt is being prepared, not yet confirmed.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Receipt has been confirmed and stock adjusted.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Receipt has been cancelled.
    /// </summary>
    Cancelled = 2
}
