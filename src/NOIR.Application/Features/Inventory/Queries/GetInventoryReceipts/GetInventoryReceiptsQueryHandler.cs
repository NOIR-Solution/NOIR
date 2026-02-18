using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;

namespace NOIR.Application.Features.Inventory.Queries.GetInventoryReceipts;

/// <summary>
/// Wolverine handler for getting paginated inventory receipts.
/// </summary>
public class GetInventoryReceiptsQueryHandler
{
    private readonly IRepository<InventoryReceipt, Guid> _repository;

    public GetInventoryReceiptsQueryHandler(IRepository<InventoryReceipt, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<InventoryReceiptSummaryDto>>> Handle(
        GetInventoryReceiptsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new InventoryReceiptsListSpec(skip, query.PageSize, query.Type, query.Status);
        var receipts = await _repository.ListAsync(spec, cancellationToken);

        var countSpec = new InventoryReceiptsCountSpec(query.Type, query.Status);
        var totalCount = await _repository.CountAsync(countSpec, cancellationToken);

        var items = receipts.Select(InventoryReceiptMapper.ToSummaryDto).ToList();

        var result = PagedResult<InventoryReceiptSummaryDto>.Create(items, totalCount, query.Page - 1, query.PageSize);

        return Result.Success(result);
    }
}
