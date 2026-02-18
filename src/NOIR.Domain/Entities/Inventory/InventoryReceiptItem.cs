namespace NOIR.Domain.Entities.Inventory;

/// <summary>
/// Line item in an inventory receipt.
/// </summary>
public class InventoryReceiptItem : TenantEntity<Guid>
{
    private InventoryReceiptItem() { }

    /// <summary>
    /// Parent receipt ID.
    /// </summary>
    public Guid InventoryReceiptId { get; private set; }

    /// <summary>
    /// Product variant being received/shipped.
    /// </summary>
    public Guid ProductVariantId { get; private set; }

    /// <summary>
    /// Parent product ID (denormalized for querying).
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Product name snapshot at time of receipt.
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// Variant name snapshot.
    /// </summary>
    public string VariantName { get; private set; } = string.Empty;

    /// <summary>
    /// SKU snapshot.
    /// </summary>
    public string? Sku { get; private set; }

    /// <summary>
    /// Quantity being received/shipped.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Unit cost for this item.
    /// </summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Computed line total: Quantity * UnitCost.
    /// </summary>
    public decimal LineTotal => Quantity * UnitCost;

    // Navigation
    public virtual InventoryReceipt Receipt { get; private set; } = null!;

    internal static InventoryReceiptItem Create(
        Guid receiptId,
        Guid productVariantId,
        Guid productId,
        string productName,
        string variantName,
        string? sku,
        int quantity,
        decimal unitCost,
        string? tenantId)
    {
        return new InventoryReceiptItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InventoryReceiptId = receiptId,
            ProductVariantId = productVariantId,
            ProductId = productId,
            ProductName = productName,
            VariantName = variantName,
            Sku = sku,
            Quantity = quantity,
            UnitCost = unitCost
        };
    }
}
