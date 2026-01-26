namespace NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;

/// <summary>
/// Wolverine handler for setting a product image as primary.
/// </summary>
public class SetPrimaryProductImageCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryProductImageCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        SetPrimaryProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking, images, and category loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-031"));
        }

        // Find the image
        var image = product.Images.FirstOrDefault(i => i.Id == command.ImageId);
        if (image is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Image with ID '{command.ImageId}' not found.", "NOIR-PRODUCT-032"));
        }

        // Set as primary
        product.SetPrimaryImage(command.ImageId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Use navigation property already loaded by spec
        return Result.Success(ProductMapper.ToDtoWithCollections(
            product,
            product.Category?.Name,
            product.Category?.Slug));
    }
}
