namespace NOIR.Application.Features.Products.Commands.AddProductImage;

/// <summary>
/// Wolverine handler for adding an image to a product.
/// </summary>
public class AddProductImageCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddProductImageCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductImageDto>> Handle(
        AddProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductImageDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-026"));
        }

        // Add image to product
        var image = product.AddImage(command.Url, command.AltText, command.IsPrimary);
        image.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(image));
    }
}
