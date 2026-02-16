namespace NOIR.Application.Features.Products.Specifications;

/// <summary>
/// Specification to retrieve product categories with optional filtering.
/// </summary>
public sealed class ProductCategoriesSpec : Specification<ProductCategory>
{
    public ProductCategoriesSpec(string? search = null, bool includeChildren = false)
    {
        Query.Where(c => string.IsNullOrEmpty(search) ||
                         c.Name.Contains(search) ||
                         (c.Description != null && c.Description.Contains(search)))
             .OrderBy(c => c.SortOrder)
             .ThenBy(c => c.Name)
             .TagWith("GetProductCategories");

        if (includeChildren)
        {
            Query.Include(c => c.Children);
        }
    }
}

/// <summary>
/// Specification to retrieve top-level product categories (no parent).
/// </summary>
public sealed class TopLevelProductCategoriesSpec : Specification<ProductCategory>
{
    public TopLevelProductCategoriesSpec()
    {
        Query.Where(c => c.ParentId == null)
             .Include(c => c.Children)
             .OrderBy(c => c.SortOrder)
             .ThenBy(c => c.Name)
             .TagWith("GetTopLevelProductCategories");
    }
}

/// <summary>
/// Specification to find a product category by ID.
/// </summary>
public sealed class ProductCategoryByIdSpec : Specification<ProductCategory>
{
    public ProductCategoryByIdSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .AsSplitQuery() // Prevent Cartesian explosion with parent + children
             .Include(c => c.Parent!)
             .Include(c => c.Children)
             .TagWith("GetProductCategoryById");
    }
}

/// <summary>
/// Specification to find a product category by ID for update (with tracking).
/// </summary>
public sealed class ProductCategoryByIdForUpdateSpec : Specification<ProductCategory>
{
    public ProductCategoryByIdForUpdateSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .AsTracking()
             .TagWith("GetProductCategoryByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a product category by slug.
/// </summary>
public sealed class ProductCategoryBySlugSpec : Specification<ProductCategory>
{
    public ProductCategoryBySlugSpec(string slug, string? tenantId = null)
    {
        Query.Where(c => c.Slug == slug.ToLowerInvariant())
             .Where(c => tenantId == null || c.TenantId == tenantId)
             .AsSplitQuery() // Prevent Cartesian explosion with parent + children
             .Include(c => c.Parent!)
             .Include(c => c.Children)
             .TagWith("GetProductCategoryBySlug");
    }
}

/// <summary>
/// Specification to check if a product category slug is unique within a tenant.
/// </summary>
public sealed class ProductCategorySlugExistsSpec : Specification<ProductCategory>
{
    public ProductCategorySlugExistsSpec(string slug, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(c => c.Slug == slug.ToLowerInvariant())
             .Where(c => tenantId == null || c.TenantId == tenantId)
             .Where(c => excludeId == null || c.Id != excludeId)
             .TagWith("CheckProductCategorySlugExists");
    }
}

/// <summary>
/// Specification to check if a product category has any products.
/// </summary>
public sealed class ProductCategoryHasProductsSpec : Specification<Product>
{
    public ProductCategoryHasProductsSpec(Guid categoryId)
    {
        Query.Where(p => p.CategoryId == categoryId)
             .TagWith("CheckProductCategoryHasProducts");
    }
}

/// <summary>
/// Specification to check if a product category has any child categories.
/// More efficient than loading all categories and filtering in memory.
/// </summary>
public sealed class ProductCategoryHasChildrenSpec : Specification<ProductCategory>
{
    public ProductCategoryHasChildrenSpec(Guid parentId)
    {
        Query.Where(c => c.ParentId == parentId)
             .TagWith("CheckProductCategoryHasChildren");
    }
}

/// <summary>
/// Specification to load multiple product categories by IDs for bulk update (with tracking).
/// </summary>
public sealed class ProductCategoriesByIdsForUpdateSpec : Specification<ProductCategory>
{
    public ProductCategoriesByIdsForUpdateSpec(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        Query.Where(c => idList.Contains(c.Id))
             .AsTracking()
             .TagWith("GetProductCategoriesByIdsForUpdate");
    }
}
