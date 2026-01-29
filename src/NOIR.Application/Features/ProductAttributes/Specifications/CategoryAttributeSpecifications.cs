namespace NOIR.Application.Features.ProductAttributes.Specifications;

/// <summary>
/// Specification to find a category-attribute link by ID.
/// </summary>
public sealed class CategoryAttributeByIdSpec : Specification<CategoryAttribute>
{
    public CategoryAttributeByIdSpec(Guid id)
    {
        Query.Where(ca => ca.Id == id)
             .Include(ca => ca.Category)
             .Include(ca => ca.Attribute)
             .TagWith("GetCategoryAttributeById");
    }
}

/// <summary>
/// Specification to find a category-attribute link by ID for update (with tracking).
/// </summary>
public sealed class CategoryAttributeByIdForUpdateSpec : Specification<CategoryAttribute>
{
    public CategoryAttributeByIdForUpdateSpec(Guid id)
    {
        Query.Where(ca => ca.Id == id)
             .AsTracking()
             .TagWith("GetCategoryAttributeByIdForUpdate");
    }
}

/// <summary>
/// Specification to retrieve all attributes for a category.
/// </summary>
public sealed class CategoryAttributesByCategoryIdSpec : Specification<CategoryAttribute>
{
    public CategoryAttributesByCategoryIdSpec(Guid categoryId)
    {
        Query.Where(ca => ca.CategoryId == categoryId)
             .Include(ca => ca.Attribute)
             .Include("Attribute.Values")  // String-based include for nested navigation
             .OrderBy(ca => ca.SortOrder)
             .TagWith("GetCategoryAttributesByCategoryId");
    }
}

/// <summary>
/// Specification to retrieve all category links for an attribute.
/// </summary>
public sealed class CategoryAttributesByAttributeIdSpec : Specification<CategoryAttribute>
{
    public CategoryAttributesByAttributeIdSpec(Guid attributeId)
    {
        Query.Where(ca => ca.AttributeId == attributeId)
             .Include(ca => ca.Category)
             .OrderBy(ca => ca.Category.Name)
             .TagWith("GetCategoryAttributesByAttributeId");
    }
}

/// <summary>
/// Specification to check if a category-attribute link exists.
/// </summary>
public sealed class CategoryAttributeLinkExistsSpec : Specification<CategoryAttribute>
{
    public CategoryAttributeLinkExistsSpec(Guid categoryId, Guid attributeId)
    {
        Query.Where(ca => ca.CategoryId == categoryId && ca.AttributeId == attributeId)
             .TagWith("CheckCategoryAttributeLinkExists");
    }
}

/// <summary>
/// Specification to retrieve required attributes for a category.
/// </summary>
public sealed class RequiredCategoryAttributesSpec : Specification<CategoryAttribute>
{
    public RequiredCategoryAttributesSpec(Guid categoryId)
    {
        Query.Where(ca => ca.CategoryId == categoryId && ca.IsRequired)
             .Include(ca => ca.Attribute)
             .OrderBy(ca => ca.SortOrder)
             .TagWith("GetRequiredCategoryAttributes");
    }
}
