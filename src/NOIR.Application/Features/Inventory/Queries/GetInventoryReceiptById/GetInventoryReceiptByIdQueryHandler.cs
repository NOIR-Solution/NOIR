using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;

namespace NOIR.Application.Features.Inventory.Queries.GetInventoryReceiptById;

/// <summary>
/// Wolverine handler for getting an inventory receipt by ID.
/// </summary>
public class GetInventoryReceiptByIdQueryHandler
{
    private readonly IRepository<InventoryReceipt, Guid> _repository;

    public GetInventoryReceiptByIdQueryHandler(IRepository<InventoryReceipt, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<InventoryReceiptDto>> Handle(
        GetInventoryReceiptByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new InventoryReceiptByIdSpec(query.ReceiptId);
        var receipt = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (receipt is null)
        {
            return Result.Failure<InventoryReceiptDto>(
                Error.NotFound($"Inventory receipt with ID '{query.ReceiptId}' not found.", "NOIR-INVENTORY-003"));
        }

        return Result.Success(InventoryReceiptMapper.ToDto(receipt));
    }
}
