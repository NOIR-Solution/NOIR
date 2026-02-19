namespace NOIR.Application.Features.Products.Commands.DuplicateProduct;

/// <summary>
/// Wolverine handler for duplicating a product.
/// Creates a new draft product with modified name/slug.
/// </summary>
public class DuplicateProductCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DuplicateProductCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        DuplicateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Get original product with all collections
        var spec = new ProductByIdWithCollectionsSpec(command.Id);
        var original = await _productRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (original is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound($"Product with ID '{command.Id}' not found.", "NOIR-PRODUCT-060"));
        }

        // Generate unique suffix
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString("x");
        var copyName = $"{original.Name} (Copy)";
        var copySlug = $"{original.Slug}-copy-{timestamp}";

        // Create duplicate product (starts as Draft)
        var duplicate = Product.Create(
            copyName,
            copySlug,
            original.BasePrice,
            original.Currency,
            original.TenantId);

        // Copy basic info
        duplicate.UpdateBasicInfo(
            copyName,
            copySlug,
            original.ShortDescription,
            original.Description,
            original.DescriptionHtml);

        // Copy organization info
        duplicate.SetCategory(original.CategoryId);
        duplicate.SetBrand(original.Brand);

        // Copy identification (new SKU, skip barcode as it must be unique)
        var copySku = original.Sku != null ? $"{original.Sku}-COPY" : null;
        duplicate.UpdateIdentification(copySku, null);

        // Copy inventory settings
        duplicate.SetInventoryTracking(original.TrackInventory);

        // Copy SEO
        duplicate.UpdateSeo(original.MetaTitle, original.MetaDescription);

        // Copy physical properties
        duplicate.UpdatePhysicalProperties(
            original.Weight, original.WeightUnit,
            original.Length, original.Width, original.Height,
            original.DimensionUnit);

        // Copy options if requested (must be done before variants if variants reference options)
        if (command.CopyOptions && original.Options.Any())
        {
            foreach (var option in original.Options.OrderBy(o => o.SortOrder))
            {
                var newOption = duplicate.AddOption(option.Name, option.DisplayName);
                foreach (var value in option.Values.OrderBy(v => v.SortOrder))
                {
                    var newValue = newOption.AddValue(value.Value, value.DisplayValue);
                    newValue.SetColorCode(value.ColorCode);
                    newValue.SetSwatchUrl(value.SwatchUrl);
                }
            }
        }

        // Copy images if requested
        if (command.CopyImages && original.Images.Any())
        {
            foreach (var image in original.Images.OrderBy(i => i.SortOrder))
            {
                duplicate.AddImage(image.Url, image.AltText, image.IsPrimary);
            }
        }

        // Copy variants if requested
        if (command.CopyVariants && original.Variants.Any())
        {
            foreach (var variant in original.Variants.OrderBy(v => v.SortOrder))
            {
                var variantSku = variant.Sku != null ? $"{variant.Sku}-COPY" : null;
                // Add variant with 0 stock (inventory must be set up separately)
                var newVariant = duplicate.AddVariant(
                    variant.Name,
                    variant.Price,
                    variantSku,
                    variant.GetOptions());

                // Copy compare-at price (for sale display)
                if (variant.CompareAtPrice.HasValue)
                {
                    newVariant.SetCompareAtPrice(variant.CompareAtPrice);
                }

                // Copy cost price
                if (variant.CostPrice.HasValue)
                {
                    newVariant.SetCostPrice(variant.CostPrice);
                }
            }
        }

        await _productRepository.AddAsync(duplicate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get category info for DTO
        string? categoryName = null;
        string? categorySlug = null;
        if (duplicate.CategoryId.HasValue)
        {
            var categorySpec = new ProductCategoryByIdSpec(duplicate.CategoryId.Value);
            var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);
            categoryName = category?.Name;
            categorySlug = category?.Slug;
        }

        return Result.Success(ProductMapper.ToDtoWithCollections(duplicate, categoryName, categorySlug));
    }
}
