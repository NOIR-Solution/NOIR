using NOIR.Application.Features.ProductAttributes.Specifications;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.Features.Products.Queries.ExportProducts;

/// <summary>
/// Wolverine handler for exporting products as flat rows.
/// Each variant becomes a separate row for CSV export.
/// </summary>
public class ExportProductsQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly ILogger<ExportProductsQueryHandler> _logger;

    public ExportProductsQueryHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<ProductAttribute, Guid> attributeRepository,
        ILogger<ExportProductsQueryHandler> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _attributeRepository = attributeRepository;
        _logger = logger;
    }

    public async Task<Result<ExportProductsResultDto>> Handle(
        ExportProductsQuery query,
        CancellationToken cancellationToken)
    {
        // Get products with all related data
        var spec = new ProductsForExportSpec(
            query.Status,
            query.CategoryId,
            query.IncludeAttributes,
            query.IncludeImages);

        var products = await _productRepository.ListAsync(spec, cancellationToken);

        // Pre-load categories for lookup
        var categoriesSpec = new AllProductCategoriesSpec();
        var categories = await _categoryRepository.ListAsync(categoriesSpec, cancellationToken);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        // Pre-load attributes for lookup if we're including attributes
        var attributeLookup = new Dictionary<Guid, ProductAttribute>();
        if (query.IncludeAttributes)
        {
            var attributeIds = products
                .SelectMany(p => p.AttributeAssignments)
                .Select(a => a.AttributeId)
                .Distinct()
                .ToList();

            if (attributeIds.Any())
            {
                var attributesSpec = new ProductAttributesByIdsSpec(attributeIds, includeValues: false, activeOnly: false);
                var attributes = await _attributeRepository.ListAsync(attributesSpec, cancellationToken);
                attributeLookup = attributes.ToDictionary(a => a.Id, a => a);
            }
        }

        var rows = new List<ExportProductRowDto>();
        var allAttributeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            var categoryName = product.CategoryId.HasValue && categoryLookup.TryGetValue(product.CategoryId.Value, out var catName)
                ? catName
                : null;

            // Collect product-level images
            var imageUrls = query.IncludeImages && product.Images.Any()
                ? string.Join("|", product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url))
                : null;

            // Collect product-level attributes
            var attributes = new Dictionary<string, string>();
            if (query.IncludeAttributes && product.AttributeAssignments.Any())
            {
                foreach (var assignment in product.AttributeAssignments.Where(a => a.VariantId == null))
                {
                    if (attributeLookup.TryGetValue(assignment.AttributeId, out var attr) &&
                        !string.IsNullOrEmpty(assignment.DisplayValue))
                    {
                        attributes[attr.Code] = assignment.DisplayValue;
                        allAttributeCodes.Add(attr.Code);
                    }
                }
            }

            // If product has variants, create a row per variant
            if (product.Variants.Any())
            {
                foreach (var variant in product.Variants.OrderBy(v => v.SortOrder))
                {
                    // Start with product-level attributes, then add/override with variant-specific attributes
                    var variantAttributes = new Dictionary<string, string>(attributes, StringComparer.OrdinalIgnoreCase);

                    // Add variant-level attributes (if any)
                    if (query.IncludeAttributes)
                    {
                        foreach (var assignment in product.AttributeAssignments.Where(a => a.VariantId == variant.Id))
                        {
                            if (attributeLookup.TryGetValue(assignment.AttributeId, out var attr) &&
                                !string.IsNullOrEmpty(assignment.DisplayValue))
                            {
                                variantAttributes[attr.Code] = assignment.DisplayValue;
                                allAttributeCodes.Add(attr.Code);
                            }
                        }
                    }

                    rows.Add(new ExportProductRowDto(
                        Name: product.Name,
                        Slug: product.Slug,
                        Sku: variant.Sku ?? product.Sku,
                        Barcode: product.Barcode,
                        BasePrice: product.BasePrice,
                        Currency: product.Currency,
                        Status: product.Status.ToString(),
                        CategoryName: categoryName,
                        Brand: product.Brand,
                        ShortDescription: product.ShortDescription,
                        VariantName: variant.Name,
                        VariantPrice: variant.Price,
                        CompareAtPrice: variant.CompareAtPrice,
                        Stock: variant.StockQuantity,
                        Images: imageUrls,
                        Attributes: variantAttributes));
                }
            }
            else
            {
                // Product without variants - single row with default values
                rows.Add(new ExportProductRowDto(
                    Name: product.Name,
                    Slug: product.Slug,
                    Sku: product.Sku,
                    Barcode: product.Barcode,
                    BasePrice: product.BasePrice,
                    Currency: product.Currency,
                    Status: product.Status.ToString(),
                    CategoryName: categoryName,
                    Brand: product.Brand,
                    ShortDescription: product.ShortDescription,
                    VariantName: null,
                    VariantPrice: null,
                    CompareAtPrice: null,
                    Stock: 0,
                    Images: imageUrls,
                    Attributes: new Dictionary<string, string>(attributes)));
            }
        }

        // Sort attribute columns alphabetically for consistent export
        var attributeColumns = allAttributeCodes.OrderBy(c => c).ToList();

        _logger.LogInformation("Exported {ProductCount} products as {RowCount} rows with {AttrCount} attribute columns",
            products.Count, rows.Count, attributeColumns.Count);

        return Result.Success(new ExportProductsResultDto(rows, attributeColumns));
    }
}
