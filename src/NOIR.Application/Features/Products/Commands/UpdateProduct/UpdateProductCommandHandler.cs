namespace NOIR.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Wolverine handler for updating an existing product.
/// </summary>
public class UpdateProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateProductCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductDto>> Handle(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get product with tracking
        var productSpec = new ProductByIdForUpdateSpec(command.Id);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.Id}' not found.", "NOIR-PRODUCT-012"));
        }

        // Check if slug changed and is unique
        if (product.Slug != command.Slug.ToLowerInvariant())
        {
            var slugSpec = new ProductSlugExistsSpec(command.Slug, tenantId, command.Id);
            var existingProduct = await _productRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingProduct != null)
            {
                return Result.Failure<ProductDto>(
                    Error.Conflict($"A product with slug '{command.Slug}' already exists.", "NOIR-PRODUCT-010"));
            }
        }

        // Check if SKU changed and is unique
        if (!string.IsNullOrWhiteSpace(command.Sku) && product.Sku != command.Sku)
        {
            var skuSpec = new ProductSkuExistsSpec(command.Sku, tenantId, command.Id);
            var existingBySku = await _productRepository.FirstOrDefaultAsync(skuSpec, cancellationToken);
            if (existingBySku != null)
            {
                return Result.Failure<ProductDto>(
                    Error.Conflict($"A product with SKU '{command.Sku}' already exists.", "NOIR-PRODUCT-011"));
            }
        }

        // Validate category exists if specified
        string? categoryName = null;
        string? categorySlug = null;
        if (command.CategoryId.HasValue)
        {
            var categorySpec = new ProductCategoryByIdSpec(command.CategoryId.Value);
            var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);
            if (category is null)
            {
                return Result.Failure<ProductDto>(
                    Error.NotFound($"Category with ID '{command.CategoryId}' not found.", "NOIR-PRODUCT-002"));
            }
            categoryName = category.Name;
            categorySlug = category.Slug;
        }

        // Update product
        product.UpdateBasicInfo(
            command.Name,
            command.Slug,
            command.ShortDescription,
            command.Description,
            command.DescriptionHtml);

        product.UpdatePricing(command.BasePrice, command.Currency);
        product.SetCategory(command.CategoryId);

        // Set brand (prefer BrandId over legacy Brand string)
        if (command.BrandId.HasValue)
        {
            product.SetBrandId(command.BrandId);
            product.SetBrand(null); // Clear legacy brand text
        }
        else if (!string.IsNullOrWhiteSpace(command.Brand))
        {
            product.SetBrand(command.Brand);
        }
        else
        {
            product.SetBrandId(null);
            product.SetBrand(null);
        }

        product.UpdateIdentification(command.Sku, command.Barcode);
        product.SetInventoryTracking(command.TrackInventory);
        product.UpdateSeo(command.MetaTitle, command.MetaDescription);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDtoWithCollections(product, categoryName, categorySlug));
    }
}
