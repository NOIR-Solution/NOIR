namespace NOIR.Application.Features.Inventory.Specifications;

/// <summary>
/// Specification to get an inventory receipt by ID with items loaded.
/// </summary>
public sealed class InventoryReceiptByIdSpec : Specification<InventoryReceipt>
{
    public InventoryReceiptByIdSpec(Guid receiptId)
    {
        Query.Where(r => r.Id == receiptId)
            .Include(r => r.Items)
            .TagWith("InventoryReceiptById");
    }
}

/// <summary>
/// Specification to get an inventory receipt by ID for update (with tracking).
/// </summary>
public sealed class InventoryReceiptByIdForUpdateSpec : Specification<InventoryReceipt>
{
    public InventoryReceiptByIdForUpdateSpec(Guid receiptId)
    {
        Query.Where(r => r.Id == receiptId)
            .Include(r => r.Items)
            .AsTracking()
            .TagWith("InventoryReceiptByIdForUpdate");
    }
}

/// <summary>
/// Specification to get inventory receipts with pagination.
/// </summary>
public sealed class InventoryReceiptsListSpec : Specification<InventoryReceipt>
{
    public InventoryReceiptsListSpec(
        int skip = 0,
        int take = 20,
        InventoryReceiptType? type = null,
        InventoryReceiptStatus? status = null)
    {
        Query.TagWith("InventoryReceiptsList");

        if (type.HasValue)
            Query.Where(r => r.Type == type.Value);

        if (status.HasValue)
            Query.Where(r => r.Status == status.Value);

        Query.Include(r => r.Items)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification to count inventory receipts matching criteria.
/// </summary>
public sealed class InventoryReceiptsCountSpec : Specification<InventoryReceipt>
{
    public InventoryReceiptsCountSpec(
        InventoryReceiptType? type = null,
        InventoryReceiptStatus? status = null)
    {
        Query.TagWith("InventoryReceiptsCount");

        if (type.HasValue)
            Query.Where(r => r.Type == type.Value);

        if (status.HasValue)
            Query.Where(r => r.Status == status.Value);
    }
}

/// <summary>
/// Specification to get the latest receipt number for today (for sequence generation).
/// </summary>
public sealed class LatestReceiptNumberTodaySpec : Specification<InventoryReceipt>
{
    public LatestReceiptNumberTodaySpec(string prefix)
    {
        Query.Where(r => r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .TagWith("LatestReceiptNumberToday");
    }
}
