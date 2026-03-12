using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.Features.Inventory.Queries.GetStockHistory;

/// <summary>
/// Wolverine handler for getting stock movement history.
/// </summary>
public class GetStockHistoryQueryHandler
{
    private readonly IRepository<InventoryMovement, Guid> _repository;

    public GetStockHistoryQueryHandler(IRepository<InventoryMovement, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<InventoryMovementDto>>> Handle(
        GetStockHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get movements with pagination
        var spec = new StockHistoryByVariantIdSpec(
            query.ProductId,
            query.VariantId,
            skip,
            query.PageSize);

        var movements = await _repository.ListAsync(spec, cancellationToken);

        // Get total count for pagination
        var countSpec = new StockHistoryByVariantIdCountSpec(query.ProductId, query.VariantId);
        var totalCount = await _repository.CountAsync(countSpec, cancellationToken);

        var items = movements.Select(InventoryMovementMapper.ToDto).ToList();

        var pageIndex = query.Page - 1;
        var result = PagedResult<InventoryMovementDto>.Create(items, totalCount, pageIndex, query.PageSize);

        return Result.Success(result);
    }
}
