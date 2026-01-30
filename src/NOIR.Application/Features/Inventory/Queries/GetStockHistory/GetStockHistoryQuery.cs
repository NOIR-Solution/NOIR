namespace NOIR.Application.Features.Inventory.Queries.GetStockHistory;

/// <summary>
/// Query to get stock movement history for a product variant.
/// </summary>
public sealed record GetStockHistoryQuery(
    Guid ProductId,
    Guid VariantId,
    int Page = 1,
    int PageSize = 20);
