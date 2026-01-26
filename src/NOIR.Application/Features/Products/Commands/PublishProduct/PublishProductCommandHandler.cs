namespace NOIR.Application.Features.Products.Commands.PublishProduct;

/// <summary>
/// Wolverine handler for publishing a product.
/// </summary>
public class PublishProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishProductCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        PublishProductCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking
        var productSpec = new ProductByIdForUpdateSpec(command.Id);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.Id}' not found.", "NOIR-PRODUCT-012"));
        }

        // Can only publish from Draft status
        if (product.Status != ProductStatus.Draft)
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Status", $"Only products in Draft status can be published. Current status: {product.Status}", "NOIR-PRODUCT-013"));
        }

        // Publish the product
        product.Publish();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get category info for DTO
        string? categoryName = null;
        string? categorySlug = null;
        if (product.CategoryId.HasValue)
        {
            var categorySpec = new ProductCategoryByIdSpec(product.CategoryId.Value);
            var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);
            categoryName = category?.Name;
            categorySlug = category?.Slug;
        }

        return Result.Success(ProductMapper.ToDtoWithCollections(product, categoryName, categorySlug));
    }
}
