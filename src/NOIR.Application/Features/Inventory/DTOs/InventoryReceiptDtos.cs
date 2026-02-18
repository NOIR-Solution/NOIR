namespace NOIR.Application.Features.Inventory.DTOs;

/// <summary>
/// DTO for InventoryReceipt entity.
/// </summary>
public sealed record InventoryReceiptDto
{
    public Guid Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public InventoryReceiptType Type { get; init; }
    public InventoryReceiptStatus Status { get; init; }
    public string? Notes { get; init; }
    public string? ConfirmedBy { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public string? CancelledBy { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalCost { get; init; }
    public IReadOnlyList<InventoryReceiptItemDto> Items { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

/// <summary>
/// Summary DTO for inventory receipt lists.
/// </summary>
public sealed record InventoryReceiptSummaryDto
{
    public Guid Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public InventoryReceiptType Type { get; init; }
    public InventoryReceiptStatus Status { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalCost { get; init; }
    public int ItemCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

/// <summary>
/// DTO for InventoryReceiptItem entity.
/// </summary>
public sealed record InventoryReceiptItemDto
{
    public Guid Id { get; init; }
    public Guid ProductVariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal LineTotal { get; init; }
}

/// <summary>
/// Request DTO for creating an inventory receipt item.
/// </summary>
public sealed record CreateInventoryReceiptItemDto(
    Guid ProductVariantId,
    Guid ProductId,
    string ProductName,
    string VariantName,
    string? Sku,
    int Quantity,
    decimal UnitCost);
