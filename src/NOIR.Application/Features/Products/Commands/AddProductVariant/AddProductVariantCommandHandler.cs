namespace NOIR.Application.Features.Products.Commands.AddProductVariant;

/// <summary>
/// Wolverine handler for adding a variant to a product.
/// </summary>
public class AddProductVariantCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProductVariantCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

        if (command.StockQuantity > 0)
        {
            variant.SetStock(command.StockQuantity);
        }

        variant.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(variant));
    }
}
