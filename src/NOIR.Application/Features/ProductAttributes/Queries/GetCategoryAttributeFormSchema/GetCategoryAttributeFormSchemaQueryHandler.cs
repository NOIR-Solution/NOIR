namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;

/// <summary>
/// Wolverine handler for getting a category's attribute form schema.
/// Used for new product creation - returns form fields without requiring a productId.
/// </summary>
public class GetCategoryAttributeFormSchemaQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;

    public GetCategoryAttributeFormSchemaQueryHandler(
        IApplicationDbContext dbContext,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<ProductAttribute, Guid> attributeRepository)
    {
        _dbContext = dbContext;
        _categoryRepository = categoryRepository;
        _attributeRepository = attributeRepository;
    }

    public async Task<Result<CategoryAttributeFormSchemaDto>> Handle(
        GetCategoryAttributeFormSchemaQuery query,
        CancellationToken cancellationToken)
    {
        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(query.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<CategoryAttributeFormSchemaDto>(
                Error.NotFound($"Category with ID '{query.CategoryId}' not found.", ErrorCodes.Product.CategoryNotFound));
        }

        // Get attributes linked to the category
        var categoryAttributeLinks = await _dbContext.CategoryAttributes
            .Where(ca => ca.CategoryId == query.CategoryId)
            .ToListAsync(cancellationToken);

        var categoryAttributeIds = categoryAttributeLinks.Select(ca => ca.AttributeId).ToList();

        // Build dictionary of category-level required overrides and sort orders
        var categoryAttributeSettings = categoryAttributeLinks
            .ToDictionary(ca => ca.AttributeId, ca => new { ca.IsRequired, ca.SortOrder });

        // Get the actual attribute details with values
        IReadOnlyCollection<ProductAttribute> applicableAttributes;
        if (categoryAttributeIds.Any())
        {
            var attributeSpec = new ProductAttributesByIdsSpec(categoryAttributeIds, includeValues: true, activeOnly: true);
            applicableAttributes = await _attributeRepository.ListAsync(attributeSpec, cancellationToken);
        }
        else
        {
            applicableAttributes = Array.Empty<ProductAttribute>();
        }

        // Build form fields (no current values - this is for new product creation)
        var fields = applicableAttributes
            .Select(attr =>
            {
                categoryAttributeSettings.TryGetValue(attr.Id, out var categorySettings);

                // Attribute is required if either the attribute itself or the category setting says so
                var isRequired = attr.IsRequired || (categorySettings?.IsRequired ?? false);
                var sortOrder = categorySettings?.SortOrder ?? attr.SortOrder;

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
                    null, // CurrentValue - null for new products
                    null); // CurrentDisplayValue - null for new products
            })
            .OrderBy(f => categoryAttributeSettings.TryGetValue(f.AttributeId, out var s) ? s.SortOrder : int.MaxValue)
            .ThenBy(f => f.Name)
            .ToList();

        return Result.Success(new CategoryAttributeFormSchemaDto(
            query.CategoryId,
            category.Name,
            fields));
    }
}
