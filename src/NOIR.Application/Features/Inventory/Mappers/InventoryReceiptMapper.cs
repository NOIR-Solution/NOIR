using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.Features.Inventory.Mappers;

/// <summary>
/// Mapper for InventoryReceipt entity to DTO conversions.
/// </summary>
public static class InventoryReceiptMapper
{
    public static InventoryReceiptDto ToDto(InventoryReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        Type = receipt.Type,
        Status = receipt.Status,
        Notes = receipt.Notes,
        ConfirmedBy = receipt.ConfirmedBy,
        ConfirmedAt = receipt.ConfirmedAt,
        CancelledBy = receipt.CancelledBy,
        CancelledAt = receipt.CancelledAt,
        CancellationReason = receipt.CancellationReason,
        TotalQuantity = receipt.TotalQuantity,
        TotalCost = receipt.TotalCost,
        Items = receipt.Items.Select(ToDto).ToList(),
        CreatedAt = receipt.CreatedAt,
        CreatedBy = receipt.CreatedBy
    };

    public static InventoryReceiptSummaryDto ToSummaryDto(InventoryReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        Type = receipt.Type,
        Status = receipt.Status,
        TotalQuantity = receipt.TotalQuantity,
        TotalCost = receipt.TotalCost,
        ItemCount = receipt.Items.Count,
        CreatedAt = receipt.CreatedAt,
        CreatedBy = receipt.CreatedBy
    };

    public static InventoryReceiptItemDto ToDto(InventoryReceiptItem item) => new()
    {
        Id = item.Id,
        ProductVariantId = item.ProductVariantId,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        VariantName = item.VariantName,
        Sku = item.Sku,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
        LineTotal = item.LineTotal
    };
}
