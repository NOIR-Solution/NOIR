namespace NOIR.Application.Features.Products.Commands.AddProductVariant;

/// <summary>
/// Wolverine handler for adding a variant to a product.
/// </summary>
public class AddProductVariantCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryMovementLogger _movementLogger;

    public AddProductVariantCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        IInventoryMovementLogger movementLogger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _movementLogger = movementLogger;
    }

    public async Task<Result<ProductVariantDto>> Handle(
        AddProductVariantCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and variants loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductVariantDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-021"));
        }

        // Add variant to product
        var variant = product.AddVariant(
            command.Name,
            command.Price,
            command.Sku,
            command.Options);

        // Set additional properties
        if (command.CompareAtPrice.HasValue)
        {
            variant.SetCompareAtPrice(command.CompareAtPrice);
        }

        if (command.CostPrice.HasValue)
        {
            variant.SetCostPrice(command.CostPrice);
        }

        if (command.StockQuantity > 0)
        {
            variant.SetStock(command.StockQuantity);
        }

        variant.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log initial stock as StockIn if quantity > 0
        if (command.StockQuantity > 0)
        {
            await _movementLogger.LogMovementAsync(
                variant,
                InventoryMovementType.StockIn,
                quantityBefore: 0,
                quantityMoved: command.StockQuantity,
                reference: $"SKU: {variant.Sku}",
                notes: "Initial stock on variant creation",
                userId: command.UserId,
                cancellationToken: cancellationToken);
        }

        return Result.Success(ProductMapper.ToDto(variant));
    }
}
