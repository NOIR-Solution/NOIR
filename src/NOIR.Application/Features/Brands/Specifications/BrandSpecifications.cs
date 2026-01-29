namespace NOIR.Application.Features.Brands.Specifications;

/// <summary>
/// Specification to find a brand by ID.
/// </summary>
public sealed class BrandByIdSpec : Specification<Brand>
{
    public BrandByIdSpec(Guid id)
    {
        Query.Where(b => b.Id == id)
             .TagWith("GetBrandById");
    }
}

/// <summary>
/// Specification to find a brand by ID for update (with tracking).
/// </summary>
public sealed class BrandByIdForUpdateSpec : Specification<Brand>
{
    public BrandByIdForUpdateSpec(Guid id)
    {
        Query.Where(b => b.Id == id)
             .AsTracking()
             .TagWith("GetBrandByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a brand by slug.
/// </summary>
public sealed class BrandBySlugSpec : Specification<Brand>
{
    public BrandBySlugSpec(string slug)
    {
        Query.Where(b => b.Slug == slug.ToLowerInvariant())
             .TagWith("GetBrandBySlug");
    }
}

/// <summary>
/// Specification to check if a brand slug exists (for uniqueness validation).
/// </summary>
public sealed class BrandSlugExistsSpec : Specification<Brand>
{
    public BrandSlugExistsSpec(string slug, Guid? excludeId = null)
    {
        Query.Where(b => b.Slug == slug.ToLowerInvariant());

        if (excludeId.HasValue)
        {
            Query.Where(b => b.Id != excludeId.Value);
        }

        Query.TagWith("CheckBrandSlugExists");
    }
}

/// <summary>
/// Specification to retrieve all active brands.
/// </summary>
public sealed class ActiveBrandsSpec : Specification<Brand>
{
    public ActiveBrandsSpec(string? search = null)
    {
        Query.Where(b => b.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(b => b.Name.Contains(search) || b.Slug.Contains(search));
        }

        Query.OrderBy(b => b.SortOrder)
             .ThenBy(b => b.Name)
             .TagWith("GetActiveBrands");
    }
}

/// <summary>
/// Specification to retrieve featured brands.
/// </summary>
public sealed class FeaturedBrandsSpec : Specification<Brand>
{
    public FeaturedBrandsSpec()
    {
        Query.Where(b => b.IsActive && b.IsFeatured)
             .OrderBy(b => b.SortOrder)
             .ThenBy(b => b.Name)
             .TagWith("GetFeaturedBrands");
    }
}

/// <summary>
/// Specification for paginated brand listing with filters.
/// </summary>
public sealed class BrandsPagedSpec : Specification<Brand>
{
    public BrandsPagedSpec(string? search, bool? isActive, bool? isFeatured, int page, int pageSize)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(b => b.Name.Contains(search) || b.Slug.Contains(search));
        }

        if (isActive.HasValue)
        {
            Query.Where(b => b.IsActive == isActive.Value);
        }

        if (isFeatured.HasValue)
        {
            Query.Where(b => b.IsFeatured == isFeatured.Value);
        }

        Query.OrderBy(b => b.SortOrder)
             .ThenBy(b => b.Name)
             .Skip((page - 1) * pageSize)
             .Take(pageSize)
             .TagWith("GetBrandsPaged");
    }
}

/// <summary>
/// Specification for counting brands with filters (for pagination).
/// </summary>
public sealed class BrandsCountSpec : Specification<Brand>
{
    public BrandsCountSpec(string? search, bool? isActive, bool? isFeatured)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(b => b.Name.Contains(search) || b.Slug.Contains(search));
        }

        if (isActive.HasValue)
        {
            Query.Where(b => b.IsActive == isActive.Value);
        }

        if (isFeatured.HasValue)
        {
            Query.Where(b => b.IsFeatured == isFeatured.Value);
        }

        Query.TagWith("CountBrands");
    }
}

/// <summary>
/// Specification to check if a brand has any products.
/// </summary>
public sealed class BrandHasProductsSpec : Specification<Product>
{
    public BrandHasProductsSpec(Guid brandId)
    {
        Query.Where(p => p.BrandId == brandId)
             .TagWith("CheckBrandHasProducts");
    }
}
