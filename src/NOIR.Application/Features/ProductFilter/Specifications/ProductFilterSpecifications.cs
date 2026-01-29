using ProductFilterIndexEntity = NOIR.Domain.Entities.Product.ProductFilterIndex;

namespace NOIR.Application.Features.ProductFilter.Specifications;

/// <summary>
/// Specification to get ProductFilterIndex entries for a category and its descendants.
/// Used for facet calculation and product filtering.
/// </summary>
public sealed class ProductFilterIndexByCategorySpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexByCategorySpec(
        Guid? categoryId,
        string? categoryPath = null,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        if (categoryId.HasValue)
        {
            var categoryIdStr = categoryId.Value.ToString();
            Query.Where(p =>
                p.CategoryId == categoryId.Value ||
                (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr)));
        }

        Query.TagWith("ProductFilterIndexByCategory");
    }
}

/// <summary>
/// Specification to get ProductFilterIndex entries by brand slugs.
/// </summary>
public sealed class ProductFilterIndexByBrandsSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexByBrandsSpec(
        List<string> brandSlugs,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        if (brandSlugs.Any())
        {
            Query.Where(p => p.BrandSlug != null && brandSlugs.Contains(p.BrandSlug));
        }

        Query.TagWith("ProductFilterIndexByBrands");
    }
}

/// <summary>
/// Specification to get ProductFilterIndex entries within a price range.
/// </summary>
public sealed class ProductFilterIndexByPriceRangeSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexByPriceRangeSpec(
        decimal? minPrice,
        decimal? maxPrice,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        // Products are in range if their price range overlaps with the filter range
        if (minPrice.HasValue)
        {
            Query.Where(p => p.MaxPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            Query.Where(p => p.MinPrice <= maxPrice.Value);
        }

        Query.TagWith("ProductFilterIndexByPriceRange");
    }
}

/// <summary>
/// Specification to get ProductFilterIndex entries that are in stock.
/// </summary>
public sealed class ProductFilterIndexInStockSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexInStockSpec(bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        Query.Where(p => p.InStock);
        Query.TagWith("ProductFilterIndexInStock");
    }
}

/// <summary>
/// Specification to search ProductFilterIndex by text query.
/// </summary>
public sealed class ProductFilterIndexSearchSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexSearchSpec(
        string searchQuery,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            var searchTerm = searchQuery.ToLower();
            Query.Where(p =>
                p.SearchText.ToLower().Contains(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm));
        }

        Query.TagWith("ProductFilterIndexSearch");
    }
}

/// <summary>
/// Specification to count ProductFilterIndex entries for brand facets.
/// </summary>
public sealed class ProductFilterIndexBrandCountSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexBrandCountSpec(bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        Query.Where(p => p.BrandId != null && p.BrandSlug != null);
        Query.TagWith("ProductFilterIndexBrandCount");
    }
}

/// <summary>
/// Specification to get price range statistics for ProductFilterIndex entries.
/// </summary>
public sealed class ProductFilterIndexPriceStatsSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexPriceStatsSpec(
        Guid? categoryId = null,
        bool activeOnly = true)
    {
        if (activeOnly)
        {
            Query.Where(p => p.Status == ProductStatus.Active);
        }

        if (categoryId.HasValue)
        {
            var categoryIdStr = categoryId.Value.ToString();
            Query.Where(p =>
                p.CategoryId == categoryId.Value ||
                (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr)));
        }

        Query.TagWith("ProductFilterIndexPriceStats");
    }
}

/// <summary>
/// Specification to get ProductFilterIndex by product ID.
/// Used for index synchronization.
/// </summary>
public sealed class ProductFilterIndexByProductIdSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexByProductIdSpec(Guid productId)
    {
        Query.Where(p => p.ProductId == productId)
             .AsTracking()
             .TagWith("ProductFilterIndexByProductId");
    }
}

/// <summary>
/// Specification to get ProductFilterIndex entries with attribute filtering.
/// Combines multiple filter criteria for the main product filter query.
/// </summary>
public sealed class ProductFilterIndexFilteredSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexFilteredSpec(
        string? categorySlug,
        Guid? categoryId,
        string? categoryPath,
        List<string>? brands,
        decimal? priceMin,
        decimal? priceMax,
        bool inStockOnly,
        string? searchQuery,
        int skip,
        int take)
    {
        // Active products only
        Query.Where(p => p.Status == ProductStatus.Active);

        // Category filter (with hierarchy support)
        if (categoryId.HasValue)
        {
            var categoryIdStr = categoryId.Value.ToString();
            Query.Where(p =>
                p.CategoryId == categoryId.Value ||
                (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr)));
        }

        // Brand filter (OR within brands)
        if (brands?.Any() == true)
        {
            Query.Where(p => p.BrandSlug != null && brands.Contains(p.BrandSlug));
        }

        // Price range filter
        if (priceMin.HasValue)
        {
            Query.Where(p => p.MaxPrice >= priceMin.Value);
        }
        if (priceMax.HasValue)
        {
            Query.Where(p => p.MinPrice <= priceMax.Value);
        }

        // In-stock filter
        if (inStockOnly)
        {
            Query.Where(p => p.InStock);
        }

        // Search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            var searchTerm = searchQuery.ToLower();
            Query.Where(p =>
                p.SearchText.ToLower().Contains(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm));
        }

        // Pagination
        Query.Skip(skip).Take(take);

        Query.TagWith("ProductFilterIndexFiltered");
    }
}

/// <summary>
/// Specification to count ProductFilterIndex entries with filters applied.
/// Used for pagination total count.
/// </summary>
public sealed class ProductFilterIndexCountSpec : Specification<ProductFilterIndexEntity>
{
    public ProductFilterIndexCountSpec(
        Guid? categoryId,
        List<string>? brands,
        decimal? priceMin,
        decimal? priceMax,
        bool inStockOnly,
        string? searchQuery)
    {
        // Active products only
        Query.Where(p => p.Status == ProductStatus.Active);

        // Category filter (with hierarchy support)
        if (categoryId.HasValue)
        {
            var categoryIdStr = categoryId.Value.ToString();
            Query.Where(p =>
                p.CategoryId == categoryId.Value ||
                (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr)));
        }

        // Brand filter (OR within brands)
        if (brands?.Any() == true)
        {
            Query.Where(p => p.BrandSlug != null && brands.Contains(p.BrandSlug));
        }

        // Price range filter
        if (priceMin.HasValue)
        {
            Query.Where(p => p.MaxPrice >= priceMin.Value);
        }
        if (priceMax.HasValue)
        {
            Query.Where(p => p.MinPrice <= priceMax.Value);
        }

        // In-stock filter
        if (inStockOnly)
        {
            Query.Where(p => p.InStock);
        }

        // Search filter
        if (!string.IsNullOrEmpty(searchQuery))
        {
            var searchTerm = searchQuery.ToLower();
            Query.Where(p =>
                p.SearchText.ToLower().Contains(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm));
        }

        Query.TagWith("ProductFilterIndexCount");
    }
}
