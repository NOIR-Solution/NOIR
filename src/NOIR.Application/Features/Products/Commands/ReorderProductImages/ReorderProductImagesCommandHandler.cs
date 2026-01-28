namespace NOIR.Application.Features.Products.Commands.ReorderProductImages;

/// <summary>
/// Handler for reordering product images in bulk.
/// </summary>
public class ReorderProductImagesCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderProductImagesCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        ReorderProductImagesCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-026"));
        }

        // Validate all image IDs exist in the product
        var productImageIds = product.Images.Select(i => i.Id).ToHashSet();
        var requestedImageIds = command.Items.Select(i => i.ImageId).ToHashSet();
        var invalidIds = requestedImageIds.Except(productImageIds).ToList();

        if (invalidIds.Count > 0)
        {
            return Result.Failure<ProductDto>(
                Error.Validation(
                    "imageIds",
                    $"Invalid image IDs: {string.Join(", ", invalidIds)}",
                    "NOIR-PRODUCT-033"));
        }

        // Update sort orders
        foreach (var item in command.Items)
        {
            var image = product.Images.First(i => i.Id == item.ImageId);
            image.SetSortOrder(item.SortOrder);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(product));
    }
}
