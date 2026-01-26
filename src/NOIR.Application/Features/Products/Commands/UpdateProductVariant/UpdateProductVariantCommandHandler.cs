namespace NOIR.Application.Features.Products.Commands.UpdateProductVariant;

/// <summary>
/// Wolverine handler for updating a product variant.
/// </summary>
public class UpdateProductVariantCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductVariantDto>> Handle(
        UpdateProductVariantCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and variants loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductVariantDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-022"));
        }

        // Find the variant
        var variant = product.Variants.FirstOrDefault(v => v.Id == command.VariantId);
        if (variant is null)
        {
            return Result.Failure<ProductVariantDto>(
                Error.NotFound($"Variant with ID '{command.VariantId}' not found.", "NOIR-PRODUCT-023"));
        }

        // Update variant
        variant.UpdateDetails(command.Name, command.Price, command.Sku);
        variant.SetCompareAtPrice(command.CompareAtPrice);
        variant.SetStock(command.StockQuantity);
        variant.SetSortOrder(command.SortOrder);

        if (command.Options is not null)
        {
            variant.UpdateOptions(command.Options);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(variant));
    }
}
