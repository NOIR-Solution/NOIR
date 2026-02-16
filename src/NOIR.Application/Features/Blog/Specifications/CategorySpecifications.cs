namespace NOIR.Application.Features.Blog.Specifications;

/// <summary>
/// Specification to retrieve categories with optional filtering.
/// </summary>
public sealed class CategoriesSpec : Specification<PostCategory>
{
    public CategoriesSpec(string? search = null, bool includeChildren = false)
    {
        Query.Where(c => string.IsNullOrEmpty(search) ||
                         c.Name.Contains(search) ||
                         (c.Description != null && c.Description.Contains(search)))
             .OrderBy(c => c.SortOrder)
             .ThenBy(c => c.Name)
             .TagWith("GetCategories");

        if (includeChildren)
        {
            Query.Include(c => c.Children);
        }
    }
}

/// <summary>
/// Specification to retrieve top-level categories (no parent).
/// </summary>
public sealed class TopLevelCategoriesSpec : Specification<PostCategory>
{
    public TopLevelCategoriesSpec()
    {
        Query.Where(c => c.ParentId == null)
             .Include(c => c.Children)
             .OrderBy(c => c.SortOrder)
             .ThenBy(c => c.Name)
             .TagWith("GetTopLevelCategories");
    }
}

/// <summary>
/// Specification to find a category by ID.
/// </summary>
public sealed class CategoryByIdSpec : Specification<PostCategory>
{
    public CategoryByIdSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .Include(c => c.Parent!)
             .Include(c => c.Children)
             .TagWith("GetCategoryById");
    }
}

/// <summary>
/// Specification to find a category by ID for update (with tracking).
/// </summary>
public sealed class CategoryByIdForUpdateSpec : Specification<PostCategory>
{
    public CategoryByIdForUpdateSpec(Guid id)
    {
        Query.Where(c => c.Id == id)
             .AsTracking()
             .TagWith("GetCategoryByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a category by slug.
/// </summary>
public sealed class CategoryBySlugSpec : Specification<PostCategory>
{
    public CategoryBySlugSpec(string slug, string? tenantId = null)
    {
        Query.Where(c => c.Slug == slug.ToLowerInvariant())
             .Where(c => tenantId == null || c.TenantId == tenantId)
             .Include(c => c.Parent!)
             .Include(c => c.Children)
             .TagWith("GetCategoryBySlug");
    }
}

/// <summary>
/// Specification to check if a category slug is unique within a tenant.
/// </summary>
public sealed class CategorySlugExistsSpec : Specification<PostCategory>
{
    public CategorySlugExistsSpec(string slug, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(c => c.Slug == slug.ToLowerInvariant())
             .Where(c => tenantId == null || c.TenantId == tenantId)
             .Where(c => excludeId == null || c.Id != excludeId)
             .TagWith("CheckCategorySlugExists");
    }
}

/// <summary>
/// Specification to check if a category has any posts.
/// </summary>
public sealed class CategoryHasPostsSpec : Specification<Post>
{
    public CategoryHasPostsSpec(Guid categoryId)
    {
        Query.Where(p => p.CategoryId == categoryId)
             .TagWith("CheckCategoryHasPosts");
    }
}

/// <summary>
/// Specification to check if a category has any child categories.
/// </summary>
public sealed class CategoryHasChildrenSpec : Specification<PostCategory>
{
    public CategoryHasChildrenSpec(Guid parentId)
    {
        Query.Where(c => c.ParentId == parentId)
             .TagWith("CheckCategoryHasChildren");
    }
}

/// <summary>
/// Specification to retrieve multiple categories by IDs for bulk update (with tracking).
/// </summary>
public sealed class CategoriesByIdsForUpdateSpec : Specification<PostCategory>
{
    public CategoriesByIdsForUpdateSpec(List<Guid> ids)
    {
        Query.Where(c => ids.Contains(c.Id))
             .AsTracking()
             .TagWith("GetCategoriesByIdsForUpdate");
    }
}

/// <summary>
/// Specification to retrieve all active (non-deleted) categories for sitemap.
/// </summary>
public sealed class ActiveCategoriesSpec : Specification<PostCategory>
{
    public ActiveCategoriesSpec()
    {
        Query.OrderBy(c => c.SortOrder)
             .ThenBy(c => c.Name)
             .TagWith("GetActiveCategories");
    }
}
