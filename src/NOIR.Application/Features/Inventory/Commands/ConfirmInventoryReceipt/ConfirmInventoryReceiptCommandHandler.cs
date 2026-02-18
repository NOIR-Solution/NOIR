using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;

namespace NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;

/// <summary>
/// Wolverine handler for confirming an inventory receipt.
/// Adjusts stock for all items in the receipt.
/// </summary>
public class ConfirmInventoryReceiptCommandHandler
{
    private readonly IRepository<InventoryReceipt, Guid> _receiptRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IInventoryMovementLogger _movementLogger;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmInventoryReceiptCommandHandler(
        IRepository<InventoryReceipt, Guid> receiptRepository,
        IRepository<Product, Guid> productRepository,
        IInventoryMovementLogger movementLogger,
        IUnitOfWork unitOfWork)
    {
        _receiptRepository = receiptRepository;
        _productRepository = productRepository;
        _movementLogger = movementLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryReceiptDto>> Handle(
        ConfirmInventoryReceiptCommand command,
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
            receipt.Confirm(command.UserId!);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<InventoryReceiptDto>(
                Error.Validation("Status", ex.Message, "NOIR-INVENTORY-004"));
        }

        // Adjust stock for each item
        foreach (var item in receipt.Items)
        {
            var productSpec = new ProductByIdForUpdateSpec(item.ProductId);
            var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

            if (product is null) continue;

            var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
            if (variant is null) continue;

            var quantityBefore = variant.StockQuantity;
            int quantityMoved;

            if (receipt.Type == InventoryReceiptType.StockIn)
            {
                variant.AdjustStock(item.Quantity);
                quantityMoved = item.Quantity;
            }
            else
            {
                try
                {
                    variant.ReserveStock(item.Quantity);
                }
                catch (InvalidOperationException)
                {
                    return Result.Failure<InventoryReceiptDto>(
                        Error.Validation("Quantity",
                            $"Insufficient stock for {item.ProductName} - {item.VariantName}. Available: {variant.StockQuantity}, Requested: {item.Quantity}",
                            "NOIR-INVENTORY-002"));
                }
                quantityMoved = -item.Quantity;
            }

            var movementType = receipt.Type == InventoryReceiptType.StockIn
                ? InventoryMovementType.StockIn
                : InventoryMovementType.StockOut;

            await _movementLogger.LogMovementAsync(
                variant,
                movementType,
                quantityBefore,
                quantityMoved,
                reference: receipt.ReceiptNumber,
                notes: $"From receipt {receipt.ReceiptNumber}",
                userId: command.UserId,
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InventoryReceiptMapper.ToDto(receipt));
    }
}
