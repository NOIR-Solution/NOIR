namespace NOIR.Application.Features.ProductAttributes.Specifications;

/// <summary>
/// Specification to find a product attribute by ID.
/// </summary>
public sealed class ProductAttributeByIdSpec : Specification<ProductAttribute>
{
    public ProductAttributeByIdSpec(Guid id, bool includeValues = false)
    {
        Query.Where(a => a.Id == id);

        if (includeValues)
        {
            Query.Include(a => a.Values);
        }

        Query.TagWith("GetProductAttributeById");
    }
}

/// <summary>
/// Specification to find a product attribute by ID for update (with tracking).
/// </summary>
public sealed class ProductAttributeByIdForUpdateSpec : Specification<ProductAttribute>
{
    public ProductAttributeByIdForUpdateSpec(Guid id, bool includeValues = false)
    {
        Query.Where(a => a.Id == id)
             .AsTracking();

        if (includeValues)
        {
            Query.Include(a => a.Values);
        }

        Query.TagWith("GetProductAttributeByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a product attribute by code.
/// </summary>
public sealed class ProductAttributeByCodeSpec : Specification<ProductAttribute>
{
    public ProductAttributeByCodeSpec(string code)
    {
        Query.Where(a => a.Code == code.ToLowerInvariant().Replace(" ", "_"))
             .TagWith("GetProductAttributeByCode");
    }
}

/// <summary>
/// Specification to check if a product attribute code exists (for uniqueness validation).
/// </summary>
public sealed class ProductAttributeCodeExistsSpec : Specification<ProductAttribute>
{
    public ProductAttributeCodeExistsSpec(string code, Guid? excludeId = null)
    {
        Query.Where(a => a.Code == code.ToLowerInvariant().Replace(" ", "_"));

        if (excludeId.HasValue)
        {
            Query.Where(a => a.Id != excludeId.Value);
        }

        Query.TagWith("CheckProductAttributeCodeExists");
    }
}

/// <summary>
/// Specification to retrieve all active product attributes.
/// </summary>
public sealed class ActiveProductAttributesSpec : Specification<ProductAttribute>
{
    public ActiveProductAttributesSpec(string? search = null, bool includeValues = false)
    {
        Query.Where(a => a.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));
        }

        if (includeValues)
        {
            Query.Include(a => a.Values);
        }

        Query.OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .TagWith("GetActiveProductAttributes");
    }
}

/// <summary>
/// Specification to retrieve filterable product attributes (for filter sidebar).
/// </summary>
public sealed class FilterableProductAttributesSpec : Specification<ProductAttribute>
{
    public FilterableProductAttributesSpec()
    {
        Query.Where(a => a.IsActive && a.IsFilterable)
             .Include(a => a.Values)
             .OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .TagWith("GetFilterableProductAttributes");
    }
}

/// <summary>
/// Specification to retrieve variant product attributes.
/// </summary>
public sealed class VariantProductAttributesSpec : Specification<ProductAttribute>
{
    public VariantProductAttributesSpec()
    {
        Query.Where(a => a.IsActive && a.IsVariantAttribute)
             .Include(a => a.Values)
             .OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .TagWith("GetVariantProductAttributes");
    }
}

/// <summary>
/// Specification for paginated product attribute listing with filters.
/// </summary>
public sealed class ProductAttributesPagedSpec : Specification<ProductAttribute>
{
    public ProductAttributesPagedSpec(
        string? search,
        bool? isActive,
        bool? isFilterable,
        bool? isVariantAttribute,
        string? type,
        int page,
        int pageSize,
        bool includeValues = false)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));
        }

        if (isActive.HasValue)
        {
            Query.Where(a => a.IsActive == isActive.Value);
        }

        if (isFilterable.HasValue)
        {
            Query.Where(a => a.IsFilterable == isFilterable.Value);
        }

        if (isVariantAttribute.HasValue)
        {
            Query.Where(a => a.IsVariantAttribute == isVariantAttribute.Value);
        }

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<AttributeType>(type, true, out var attributeType))
        {
            Query.Where(a => a.Type == attributeType);
        }

        if (includeValues)
        {
            Query.Include(a => a.Values);
        }

        Query.OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .TagWith("GetProductAttributesPaged");
    }
}

/// <summary>
/// Specification for counting product attributes with filters (for pagination).
/// </summary>
public sealed class ProductAttributesCountSpec : Specification<ProductAttribute>
{
    public ProductAttributesCountSpec(
        string? search,
        bool? isActive,
        bool? isFilterable,
        bool? isVariantAttribute,
        string? type)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));
        }

        if (isActive.HasValue)
        {
            Query.Where(a => a.IsActive == isActive.Value);
        }

        if (isFilterable.HasValue)
        {
            Query.Where(a => a.IsFilterable == isFilterable.Value);
        }

        if (isVariantAttribute.HasValue)
        {
            Query.Where(a => a.IsVariantAttribute == isVariantAttribute.Value);
        }

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<AttributeType>(type, true, out var attributeType))
        {
            Query.Where(a => a.Type == attributeType);
        }

        Query.TagWith("CountProductAttributes");
    }
}

/// <summary>
/// Specification to retrieve product attributes by type.
/// </summary>
public sealed class ProductAttributesByTypeSpec : Specification<ProductAttribute>
{
    public ProductAttributesByTypeSpec(AttributeType type)
    {
        Query.Where(a => a.IsActive && a.Type == type)
             .Include(a => a.Values)
             .OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .TagWith("GetProductAttributesByType");
    }
}

/// <summary>
/// Specification to retrieve active product attributes by a list of IDs.
/// Used for getting category-specific attributes.
/// </summary>
public sealed class ProductAttributesByIdsSpec : Specification<ProductAttribute>
{
    public ProductAttributesByIdsSpec(IEnumerable<Guid> ids, bool includeValues = false, bool activeOnly = true)
    {
        var idList = ids.ToList();
        Query.Where(a => idList.Contains(a.Id));

        if (activeOnly)
        {
            Query.Where(a => a.IsActive);
        }

        if (includeValues)
        {
            Query.Include(a => a.Values.Where(v => v.IsActive));
        }

        Query.OrderBy(a => a.SortOrder)
             .ThenBy(a => a.Name)
             .TagWith("GetProductAttributesByIds");
    }
}
