namespace NOIR.Application.Features.Inventory.Queries.GetInventoryReceiptById;

/// <summary>
/// Query to get an inventory receipt by ID.
/// </summary>
public sealed record GetInventoryReceiptByIdQuery(Guid ReceiptId);
