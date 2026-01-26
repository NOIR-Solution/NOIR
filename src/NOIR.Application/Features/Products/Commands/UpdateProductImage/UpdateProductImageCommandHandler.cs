namespace NOIR.Application.Features.Products.Commands.UpdateProductImage;

/// <summary>
/// Wolverine handler for updating a product image.
/// </summary>
public class UpdateProductImageCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductImageCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-027"));
        }

        // Find the image
        var image = product.Images.FirstOrDefault(i => i.Id == command.ImageId);
        if (image is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Image with ID '{command.ImageId}' not found.", "NOIR-PRODUCT-028"));
        }

        // Update image
        image.Update(command.Url, command.AltText);
        image.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDtoWithCollections(
            product,
            product.Category?.Name,
            product.Category?.Slug));
    }
}
