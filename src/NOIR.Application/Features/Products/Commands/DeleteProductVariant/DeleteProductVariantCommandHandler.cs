namespace NOIR.Application.Features.Products.Commands.DeleteProductVariant;

/// <summary>
/// Wolverine handler for deleting a product variant.
/// </summary>
public class DeleteProductVariantCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductVariantCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteProductVariantCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and variants loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-024"));
        }

        // Find the variant
        var variant = product.Variants.FirstOrDefault(v => v.Id == command.VariantId);
        if (variant is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Variant with ID '{command.VariantId}' not found.", "NOIR-PRODUCT-025"));
        }

        // Remove variant from product
        product.RemoveVariant(command.VariantId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
