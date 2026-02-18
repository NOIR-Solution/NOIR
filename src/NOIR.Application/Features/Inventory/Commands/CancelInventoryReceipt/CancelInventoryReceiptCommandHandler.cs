using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;

namespace NOIR.Application.Features.Inventory.Commands.CancelInventoryReceipt;

/// <summary>
/// Wolverine handler for cancelling an inventory receipt.
/// </summary>
public class CancelInventoryReceiptCommandHandler
{
    private readonly IRepository<InventoryReceipt, Guid> _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelInventoryReceiptCommandHandler(
        IRepository<InventoryReceipt, Guid> receiptRepository,
        IUnitOfWork unitOfWork)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryReceiptDto>> Handle(
        CancelInventoryReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new InventoryReceiptByIdForUpdateSpec(command.ReceiptId);
        var receipt = await _receiptRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (receipt is null)
        {
            return Result.Failure<InventoryReceiptDto>(
                Error.NotFound($"Inventory receipt with ID '{command.ReceiptId}' not found.", "NOIR-INVENTORY-003"));
        }

        try
        {
            receipt.Cancel(command.UserId!, command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<InventoryReceiptDto>(
                Error.Validation("Status", ex.Message, "NOIR-INVENTORY-005"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InventoryReceiptMapper.ToDto(receipt));
    }
}
