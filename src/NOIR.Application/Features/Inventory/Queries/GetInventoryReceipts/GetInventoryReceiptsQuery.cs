namespace NOIR.Application.Features.Inventory.Queries.GetInventoryReceipts;

/// <summary>
/// Query to get paginated inventory receipts.
/// </summary>
public sealed record GetInventoryReceiptsQuery(
    int Page = 1,
    int PageSize = 20,
    InventoryReceiptType? Type = null,
    InventoryReceiptStatus? Status = null);
