using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeFormSchema;

/// <summary>
/// Wolverine handler for getting a product's attribute form schema.
/// </summary>
public class GetProductAttributeFormSchemaQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;

    public GetProductAttributeFormSchemaQueryHandler(
        IApplicationDbContext dbContext,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<ProductAttribute, Guid> attributeRepository)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _attributeRepository = attributeRepository;
    }

    public async Task<Result<ProductAttributeFormSchemaDto>> Handle(
        GetProductAttributeFormSchemaQuery query,
        CancellationToken cancellationToken)
    {
        // Verify product exists
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure<ProductAttributeFormSchemaDto>(
                Error.NotFound($"Product with ID '{query.ProductId}' not found.", ErrorCodes.Product.NotFound));
        }

        // Get category name if product has a category
        string? categoryName = null;
        if (product.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        // Get all attributes applicable to this product's category
        // If product has no category, show all active attributes
        IReadOnlyCollection<ProductAttribute> applicableAttributes;

        if (product.CategoryId.HasValue)
        {
            // Get attributes linked to the category
            var categoryAttributeIds = await _dbContext.CategoryAttributes
                .Where(ca => ca.CategoryId == product.CategoryId.Value)
                .Select(ca => ca.AttributeId)
                .ToListAsync(cancellationToken);

            if (categoryAttributeIds.Any())
            {
                var attributeSpec = new ProductAttributesByIdsSpec(categoryAttributeIds, includeValues: true, activeOnly: true);
                applicableAttributes = await _attributeRepository.ListAsync(attributeSpec, cancellationToken);
            }
            else
            {
                applicableAttributes = Array.Empty<ProductAttribute>();
            }
        }
        else
        {
            // No category - get all active attributes
            var attributeSpec = new ActiveProductAttributesSpec(search: null, includeValues: true);
            applicableAttributes = await _attributeRepository.ListAsync(attributeSpec, cancellationToken);
        }

        // If no category-specific attributes, get all active attributes as fallback
        if (!applicableAttributes.Any())
        {
            var attributeSpec = new ActiveProductAttributesSpec(search: null, includeValues: true);
            applicableAttributes = await _attributeRepository.ListAsync(attributeSpec, cancellationToken);
        }

        // Get current values for this product (and variant if specified)
        var currentAssignments = await _dbContext.ProductAttributeAssignments
            .Where(pa => pa.ProductId == query.ProductId)
            .Where(pa => query.VariantId == null ? pa.VariantId == null : pa.VariantId == query.VariantId)
            .ToListAsync(cancellationToken);

        var currentValuesDict = currentAssignments.ToDictionary(a => a.AttributeId);

        // Get category-level required overrides
        var categoryAttributeSettings = product.CategoryId.HasValue
            ? await _dbContext.CategoryAttributes
                .Where(ca => ca.CategoryId == product.CategoryId.Value)
                .ToDictionaryAsync(ca => ca.AttributeId, ca => ca.IsRequired, cancellationToken)
            : new Dictionary<Guid, bool>();

        // Build form fields
        var fields = applicableAttributes.Select(attr =>
        {
            currentValuesDict.TryGetValue(attr.Id, out var currentAssignment);
            categoryAttributeSettings.TryGetValue(attr.Id, out var categoryRequired);

            // Attribute is required if either the attribute itself or the category setting says so
            var isRequired = attr.IsRequired || categoryRequired;

            return new ProductAttributeFormFieldDto(
                attr.Id,
                attr.Code,
                attr.Name,
                attr.Type.ToString(),
                isRequired,
                attr.Unit,
                attr.Placeholder,
                attr.HelpText,
                attr.MinValue,
                attr.MaxValue,
                attr.MaxLength,
                attr.DefaultValue,
                attr.ValidationRegex,
                attr.RequiresValues
                    ? attr.Values.Select(v => new ProductAttributeValueDto(
                        v.Id,
                        v.Value,
                        v.DisplayValue,
                        v.ColorCode,
                        v.SwatchUrl,
                        v.IconUrl,
                        v.SortOrder,
                        v.IsActive,
                        v.ProductCount))
                        .OrderBy(v => v.SortOrder)
                        .ToList()
                    : null,
                currentAssignment?.GetTypedValue(),
                currentAssignment?.DisplayValue);
        }).ToList();

        return Result.Success(new ProductAttributeFormSchemaDto(
            product.Id,
            product.Name,
            product.CategoryId,
            categoryName,
            fields));
    }
}
