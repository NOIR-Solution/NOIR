namespace NOIR.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Wolverine handler for creating a new product.
/// </summary>
public class CreateProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(
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
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new ProductSlugExistsSpec(command.Slug, tenantId);
        var existingProduct = await _productRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingProduct != null)
        {
            return Result.Failure<ProductDto>(
                Error.Conflict($"A product with slug '{command.Slug}' already exists.", "NOIR-PRODUCT-010"));
        }

        // Check if SKU already exists (if provided)
        if (!string.IsNullOrWhiteSpace(command.Sku))
        {
            var skuSpec = new ProductSkuExistsSpec(command.Sku, tenantId);
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

        // Create the product
        var product = Product.Create(
            command.Name,
            command.Slug,
            command.BasePrice,
            command.Currency,
            tenantId);

        // Update basic info
        product.UpdateBasicInfo(
            command.Name,
            command.Slug,
            command.ShortDescription,
            command.Description,
            command.DescriptionHtml);

        // Set category
        if (command.CategoryId.HasValue)
        {
            product.SetCategory(command.CategoryId);
        }

        // Set brand
        if (!string.IsNullOrWhiteSpace(command.Brand))
        {
            product.SetBrand(command.Brand);
        }

        // Set identification
        product.UpdateIdentification(command.Sku, command.Barcode);

        // Set weight
        if (command.Weight.HasValue)
        {
            product.SetWeight(command.Weight);
        }

        // Set inventory tracking
        product.SetInventoryTracking(command.TrackInventory);

        // Update SEO
        product.UpdateSeo(command.MetaTitle, command.MetaDescription);

        // Add variants
        var variantDtos = new List<ProductVariantDto>();
        if (command.Variants?.Any() == true)
        {
            foreach (var variantCmd in command.Variants)
            {
                var variant = product.AddVariant(
                    variantCmd.Name,
                    variantCmd.Price,
                    variantCmd.Sku,
                    variantCmd.Options);

                variant.SetCompareAtPrice(variantCmd.CompareAtPrice);
                variant.SetStock(variantCmd.StockQuantity);
                variant.SetSortOrder(variantCmd.SortOrder);

                variantDtos.Add(ProductMapper.ToDto(variant));
            }
        }
        else
        {
            // Create default variant if none provided
            var defaultVariant = product.AddVariant("Default", command.BasePrice);
            variantDtos.Add(ProductMapper.ToDto(defaultVariant));
        }

        // Add images
        var imageDtos = new List<ProductImageDto>();
        if (command.Images?.Any() == true)
        {
            foreach (var imageCmd in command.Images)
            {
                var image = product.AddImage(
                    imageCmd.Url,
                    imageCmd.AltText,
                    imageCmd.IsPrimary);

                imageDtos.Add(ProductMapper.ToDto(image));
            }
        }

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ProductMapper.ToDto(product, categoryName, categorySlug, variantDtos, imageDtos));
    }
}
