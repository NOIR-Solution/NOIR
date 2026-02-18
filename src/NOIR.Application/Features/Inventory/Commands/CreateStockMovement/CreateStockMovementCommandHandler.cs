using NOIR.Application.Features.Inventory.DTOs;

namespace NOIR.Application.Features.Inventory.Commands.CreateStockMovement;

/// <summary>
/// Wolverine handler for creating a manual stock movement.
/// </summary>
public class CreateStockMovementCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IInventoryMovementLogger _movementLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockMovementCommandHandler(
        IRepository<Product, Guid> productRepository,
        IInventoryMovementLogger movementLogger,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _movementLogger = movementLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryMovementDto>> Handle(
        CreateStockMovementCommand command,
        CancellationToken cancellationToken)
    {
        // Validate movement type is one of the allowed manual types
        if (command.MovementType is not (InventoryMovementType.StockIn
            or InventoryMovementType.StockOut
            or InventoryMovementType.Adjustment))
        {
            return Result.Failure<InventoryMovementDto>(
                Error.Validation("MovementType", "Only StockIn, StockOut, and Adjustment are allowed for manual movements.", "NOIR-INVENTORY-001"));
        }

        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<InventoryMovementDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-022"));
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == command.ProductVariantId);
        if (variant is null)
        {
            return Result.Failure<InventoryMovementDto>(
                Error.NotFound($"Variant with ID '{command.ProductVariantId}' not found.", "NOIR-PRODUCT-023"));
        }

        var quantityBefore = variant.StockQuantity;
        int quantityMoved;

        switch (command.MovementType)
        {
            case InventoryMovementType.StockIn:
                variant.ReleaseStock(command.Quantity);
                quantityMoved = command.Quantity;
                break;

            case InventoryMovementType.StockOut:
                try
                {
                    variant.ReserveStock(command.Quantity);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<InventoryMovementDto>(
                        Error.Validation("Quantity", ex.Message, "NOIR-INVENTORY-002"));
                }
                quantityMoved = -command.Quantity;
                break;

            case InventoryMovementType.Adjustment:
                try
                {
                    variant.AdjustStock(command.Quantity);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<InventoryMovementDto>(
                        Error.Validation("Quantity", ex.Message, "NOIR-INVENTORY-002"));
                }
                quantityMoved = command.Quantity;
                break;

            default:
                return Result.Failure<InventoryMovementDto>(
                    Error.Validation("MovementType", "Invalid movement type.", "NOIR-INVENTORY-001"));
        }

        // Save the variant stock change first
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log the inventory movement (creates InventoryMovement record and saves)
        await _movementLogger.LogMovementAsync(
            variant,
            command.MovementType,
            quantityBefore,
            quantityMoved,
            reference: command.Reference,
            notes: command.Notes,
            userId: command.UserId,
            cancellationToken: cancellationToken);

        // Return a DTO with the known values
        var dto = new InventoryMovementDto(
            Guid.Empty, // ID is generated internally by the logger
            command.ProductVariantId,
            command.ProductId,
            command.MovementType,
            quantityBefore,
            quantityMoved,
            quantityBefore + quantityMoved,
            command.Reference,
            command.Notes,
            command.UserId,
            null,
            DateTimeOffset.UtcNow);

        return Result.Success(dto);
    }
}
