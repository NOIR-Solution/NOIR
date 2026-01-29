using NOIR.Application.Features.ProductFilter.DTOs;
using NOIR.Application.Features.ProductFilter.Services;
using ProductFilterIndexEntity = NOIR.Domain.Entities.Product.ProductFilterIndex;

namespace NOIR.Application.Features.ProductFilter.Queries.FilterProducts;

/// <summary>
/// Handler for filtering products using the denormalized ProductFilterIndex.
/// Optimized for high-performance faceted filtering.
/// </summary>
public class FilterProductsQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly FacetCalculator _facetCalculator;
    private readonly ILogger<FilterProductsQueryHandler> _logger;

    public FilterProductsQueryHandler(
        IApplicationDbContext dbContext,
        FacetCalculator facetCalculator,
        ILogger<FilterProductsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _facetCalculator = facetCalculator;
        _logger = logger;
    }

    public async Task<Result<FilteredProductsResult>> Handle(
        FilterProductsQuery query,
        CancellationToken ct)
    {
        _logger.LogDebug("Filtering products with query: {@Query}", query);

        // Start with base query - only active products
        var baseQuery = _dbContext.ProductFilterIndexes
            .TagWith("FilterProducts.BaseQuery")
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active);

        // Apply category filter (with hierarchy support)
        if (!string.IsNullOrEmpty(query.CategorySlug))
        {
            var category = await _dbContext.ProductCategories
                .TagWith("FilterProducts.GetCategory")
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == query.CategorySlug, ct);

            if (category != null)
            {
                // Include subcategories via CategoryPath
                var categoryIdStr = category.Id.ToString();
                baseQuery = baseQuery.Where(p =>
                    p.CategoryId == category.Id ||
                    (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr)));
            }
        }

        // Apply brand filter (OR within brands)
        if (query.Brands?.Any() == true)
        {
            baseQuery = baseQuery.Where(p =>
                p.BrandSlug != null && query.Brands.Contains(p.BrandSlug));
        }

        // Apply price range filter
        if (query.PriceMin.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.MaxPrice >= query.PriceMin.Value);
        }
        if (query.PriceMax.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.MinPrice <= query.PriceMax.Value);
        }

        // Apply in-stock filter
        if (query.InStockOnly)
        {
            baseQuery = baseQuery.Where(p => p.InStock);
        }

        // Apply full-text search
        if (!string.IsNullOrEmpty(query.SearchQuery))
        {
            var searchTerm = query.SearchQuery.ToLower();
            baseQuery = baseQuery.Where(p =>
                p.SearchText.ToLower().Contains(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm));
        }

        // Apply attribute filters using JSON contains
        var attributeFilters = query.AttributeFilters ?? new Dictionary<string, List<string>>();
        foreach (var (attrCode, values) in attributeFilters)
        {
            if (values.Any())
            {
                // Filter products that have any of the selected values for this attribute
                baseQuery = baseQuery.Where(p =>
                    p.AttributesJson.Contains($"\"{attrCode}\""));
            }
        }

        // Get total count before pagination
        var totalCount = await baseQuery
            .TagWith("FilterProducts.Count")
            .CountAsync(ct);

        // Calculate facets before applying pagination
        var facets = await _facetCalculator.CalculateFacetsAsync(
            baseQuery,
            attributeFilters,
            query.PriceMin,
            query.PriceMax,
            ct);

        // Apply sorting
        var sortedQuery = ApplySorting(baseQuery, query.Sort, query.Order);

        // Apply pagination
        var products = await sortedQuery
            .TagWith("FilterProducts.GetPage")
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new FilteredProductDto(
                p.ProductId,
                p.ProductName,
                p.ProductSlug,
                p.Status,
                p.MinPrice,
                p.MaxPrice,
                p.Currency,
                p.CategoryId,
                p.CategoryName,
                p.BrandId,
                p.BrandName,
                p.InStock,
                p.TotalStock,
                p.AverageRating,
                p.ReviewCount,
                p.PrimaryImageUrl))
            .ToListAsync(ct);

        var result = new FilteredProductsResult
        {
            Products = products,
            Total = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Facets = facets,
            AppliedFilters = attributeFilters
        };

        _logger.LogDebug("Filter returned {Count} products out of {Total}", products.Count, totalCount);

        return Result.Success(result);
    }

    private static IQueryable<ProductFilterIndexEntity> ApplySorting(
        IQueryable<ProductFilterIndexEntity> query,
        string sort,
        SortOrder order)
    {
        return sort.ToLower() switch
        {
            "price" => order == SortOrder.Ascending
                ? query.OrderBy(p => p.MinPrice)
                : query.OrderByDescending(p => p.MinPrice),
            "name" => order == SortOrder.Ascending
                ? query.OrderBy(p => p.ProductName)
                : query.OrderByDescending(p => p.ProductName),
            "rating" => order == SortOrder.Ascending
                ? query.OrderBy(p => p.AverageRating)
                : query.OrderByDescending(p => p.AverageRating),
            "popularity" => order == SortOrder.Ascending
                ? query.OrderBy(p => p.ReviewCount)
                : query.OrderByDescending(p => p.ReviewCount),
            "newest" or _ => order == SortOrder.Ascending
                ? query.OrderBy(p => p.ProductUpdatedAt)
                : query.OrderByDescending(p => p.ProductUpdatedAt)
        };
    }
}
