namespace NOIR.Application.Features.Products.Commands.UpdateProductVariant;

/// <summary>
/// Wolverine handler for updating a product variant.
/// </summary>
public class UpdateProductVariantCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryMovementLogger _movementLogger;

    public UpdateProductVariantCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        IInventoryMovementLogger movementLogger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _movementLogger = movementLogger;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductVariantCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and variants loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-022"));
        }

        // Find the variant
        var variant = product.Variants.FirstOrDefault(v => v.Id == command.VariantId);
        if (variant is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Variant with ID '{command.VariantId}' not found.", "NOIR-PRODUCT-023"));
        }

        // Capture before-state for stock changes
        var quantityBefore = variant.StockQuantity;

        // Update variant
        variant.UpdateDetails(command.Name, command.Price, command.Sku);
        variant.SetCompareAtPrice(command.CompareAtPrice);
        variant.SetCostPrice(command.CostPrice);
        variant.SetStock(command.StockQuantity);
        variant.SetSortOrder(command.SortOrder);

        if (command.Options is not null)
        {
            variant.UpdateOptions(command.Options);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log inventory movement if stock changed (after save to ensure consistency)
        if (quantityBefore != command.StockQuantity)
        {
            var quantityMoved = command.StockQuantity - quantityBefore;
            await _movementLogger.LogMovementAsync(
                variant,
                InventoryMovementType.Adjustment,
                quantityBefore,
                quantityMoved,
                reference: $"SKU: {variant.Sku}",
                notes: "Manual stock adjustment via variant update",
                userId: command.UserId,
                cancellationToken: cancellationToken);
        }

        return Result.Success(ProductMapper.ToDtoWithCollections(
            product,
            product.Category?.Name,
            product.Category?.Slug));
    }
}
