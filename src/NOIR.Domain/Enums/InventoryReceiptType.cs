namespace NOIR.Domain.Enums;

/// <summary>
/// Type of inventory receipt.
/// </summary>
public enum InventoryReceiptType
{
    /// <summary>
    /// Stock inbound (phieu nhap kho).
    /// </summary>
    StockIn = 0,

    /// <summary>
    /// Stock outbound (phieu xuat kho).
    /// </summary>
    StockOut = 1
}
