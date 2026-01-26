namespace NOIR.Application.Features.Products.Commands.ArchiveProduct;

/// <summary>
/// Wolverine handler for archiving a product.
/// </summary>
public class ArchiveProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveProductCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        ArchiveProductCommand command,
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

        // Archive the product
        product.Archive();

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
