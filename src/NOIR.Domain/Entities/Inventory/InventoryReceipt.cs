namespace NOIR.Domain.Entities.Inventory;

/// <summary>
/// Represents a batch stock movement receipt (phieu nhap/xuat kho).
/// Tracks bulk inventory changes with approval workflow.
/// </summary>
public class InventoryReceipt : TenantAggregateRoot<Guid>
{
    private InventoryReceipt() : base() { }
    private InventoryReceipt(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Auto-generated receipt number (e.g., "RCV-20260218-0001" or "SHP-20260218-0001").
    /// </summary>
    public string ReceiptNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Type of receipt: StockIn or StockOut.
    /// </summary>
    public InventoryReceiptType Type { get; private set; }

    /// <summary>
    /// Current status: Draft, Confirmed, or Cancelled.
    /// </summary>
    public InventoryReceiptStatus Status { get; private set; }

    /// <summary>
    /// Additional notes about this receipt.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// User who confirmed this receipt.
    /// </summary>
    public string? ConfirmedBy { get; private set; }

    /// <summary>
    /// When the receipt was confirmed.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// User who cancelled this receipt.
    /// </summary>
    public string? CancelledBy { get; private set; }

    /// <summary>
    /// When the receipt was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string? CancellationReason { get; private set; }

    // Navigation
    public virtual ICollection<InventoryReceiptItem> Items { get; private set; } = new List<InventoryReceiptItem>();

    // Computed
    public int TotalQuantity => Items.Sum(i => i.Quantity);
    public decimal TotalCost => Items.Sum(i => i.LineTotal);

    /// <summary>
    /// Creates a new inventory receipt.
    /// </summary>
    public static InventoryReceipt Create(
        string receiptNumber,
        InventoryReceiptType type,
        string? notes = null,
        string? tenantId = null)
    {
        return new InventoryReceipt(Guid.NewGuid(), tenantId)
        {
            ReceiptNumber = receiptNumber,
            Type = type,
            Status = InventoryReceiptStatus.Draft,
            Notes = notes
        };
    }

    /// <summary>
    /// Adds an item to the receipt.
    /// </summary>
    public InventoryReceiptItem AddItem(
        Guid productVariantId,
        Guid productId,
        string productName,
        string variantName,
        string? sku,
        int quantity,
        decimal unitCost)
    {
        if (Status != InventoryReceiptStatus.Draft)
            throw new InvalidOperationException("Cannot add items to a non-draft receipt.");

        var item = InventoryReceiptItem.Create(
            Id,
            productVariantId,
            productId,
            productName,
            variantName,
            sku,
            quantity,
            unitCost,
            TenantId);

        Items.Add(item);
        return item;
    }

    /// <summary>
    /// Confirms the receipt (stock will be adjusted).
    /// </summary>
    public void Confirm(string userId)
    {
        if (Status != InventoryReceiptStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm receipt in {Status} status.");

        if (!Items.Any())
            throw new InvalidOperationException("Cannot confirm an empty receipt.");

        Status = InventoryReceiptStatus.Confirmed;
        ConfirmedBy = userId;
        ConfirmedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cancels the receipt.
    /// </summary>
    public void Cancel(string userId, string? reason = null)
    {
        if (Status != InventoryReceiptStatus.Draft)
            throw new InvalidOperationException($"Cannot cancel receipt in {Status} status.");

        Status = InventoryReceiptStatus.Cancelled;
        CancelledBy = userId;
        CancelledAt = DateTimeOffset.UtcNow;
        CancellationReason = reason;
    }
}
