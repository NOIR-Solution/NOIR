using NOIR.Application.Features.ProductFilter.DTOs;
using NOIR.Application.Features.ProductFilter.Queries.FilterProducts;
using NOIR.Application.Features.ProductFilter.Queries.GetCategoryFilters;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Product Filter API endpoints.
/// Provides faceted filtering for products using the denormalized ProductFilterIndex.
/// </summary>
public static class ProductFilterEndpoints
{
    public static void MapProductFilterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products/filter")
            .WithTags("Product Filters")
            .AllowAnonymous(); // Filter endpoints are public for storefront

        // Filter products with facets
        group.MapGet("/", async (
            [FromQuery] string? category,
            [FromQuery] string? brands,
            [FromQuery] string? q,
            [FromQuery] decimal? priceMin,
            [FromQuery] decimal? priceMax,
            [FromQuery] bool? inStock,
            [FromQuery] string? attrs,
            [FromQuery] string? sort,
            [FromQuery] string? order,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            // Parse brands from comma-separated string
            var brandList = !string.IsNullOrEmpty(brands)
                ? brands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : null;

            // Parse attribute filters from query string format: attrs=color:red,blue;size:m,l
            var attributeFilters = ParseAttributeFilters(attrs);

            // Parse sort order
            var sortOrder = order?.ToLower() switch
            {
                "asc" => SortOrder.Ascending,
                "ascending" => SortOrder.Ascending,
                _ => SortOrder.Descending
            };

            var query = new FilterProductsQuery(
                CategorySlug: category,
                Brands: brandList,
                SearchQuery: q,
                PriceMin: priceMin,
                PriceMax: priceMax,
                InStockOnly: inStock ?? false,
                AttributeFilters: attributeFilters,
                Sort: sort ?? "newest",
                Order: sortOrder,
                Page: page ?? 1,
                PageSize: Math.Min(pageSize ?? 24, 100)); // Cap at 100 for performance

            var result = await bus.InvokeAsync<Result<FilteredProductsResult>>(query);
            return result.ToHttpResult();
        })
        .WithName("FilterProducts")
        .WithSummary("Filter products with facets")
        .WithDescription("""
            Filters products using the high-performance ProductFilterIndex.
            Returns paginated products with facets (filter options with counts).

            Query parameters:
            - category: Category slug to filter by (includes subcategories)
            - brands: Comma-separated brand slugs (e.g., "apple,samsung")
            - q: Search query (searches name, description, SKU)
            - priceMin/priceMax: Price range filter
            - inStock: Filter to in-stock products only
            - attrs: Attribute filters in format "code:value1,value2;code2:value3"
            - sort: Sort field (newest, price, name, rating, popularity)
            - order: Sort order (asc, desc)
            - page/pageSize: Pagination (max pageSize 100)
            """)
        .Produces<FilteredProductsResult>(StatusCodes.Status200OK);

        // POST version for complex filters (when URL gets too long)
        group.MapPost("/", async (
            ProductFilterRequest request,
            IMessageBus bus) =>
        {
            var query = new FilterProductsQuery(
                CategorySlug: request.CategorySlug,
                Brands: request.Brands,
                SearchQuery: request.SearchQuery,
                PriceMin: request.PriceMin,
                PriceMax: request.PriceMax,
                InStockOnly: request.InStockOnly,
                AttributeFilters: request.AttributeFilters,
                Sort: request.Sort,
                Order: request.Order,
                Page: request.Page,
                PageSize: Math.Min(request.PageSize, 100));

            var result = await bus.InvokeAsync<Result<FilteredProductsResult>>(query);
            return result.ToHttpResult();
        })
        .WithName("FilterProductsPost")
        .WithSummary("Filter products with facets (POST)")
        .WithDescription("Same as GET /filter but accepts filter parameters in request body. Use for complex filters.")
        .Produces<FilteredProductsResult>(StatusCodes.Status200OK);

        // Get available filters for a category
        var categoryGroup = app.MapGroup("/api/categories")
            .WithTags("Product Filters")
            .AllowAnonymous();

        categoryGroup.MapGet("/{slug}/filters", async (
            string slug,
            IMessageBus bus) =>
        {
            var query = new GetCategoryFiltersQuery(slug);
            var result = await bus.InvokeAsync<Result<CategoryFiltersDto>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetCategoryFilters")
        .WithSummary("Get available filters for a category")
        .WithDescription("""
            Returns all available filter options for a category.
            Includes:
            - Brand options with product counts
            - Price range (min/max available)
            - Category-specific attributes with predefined values
            - Availability filter

            Use this to build the filter sidebar before applying filters.
            """)
        .Produces<CategoryFiltersDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Parses attribute filters from query string format.
    /// Format: "color:red,blue;size:m,l" -> {"color": ["red", "blue"], "size": ["m", "l"]}
    /// </summary>
    private static Dictionary<string, List<string>>? ParseAttributeFilters(string? attrs)
    {
        if (string.IsNullOrEmpty(attrs))
            return null;

        var result = new Dictionary<string, List<string>>();

        // Split by semicolon for different attributes
        var attrPairs = attrs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pair in attrPairs)
        {
            // Split by colon for code:values
            var parts = pair.Split(':', 2);
            if (parts.Length != 2)
                continue;

            var code = parts[0].Trim().ToLower();
            var values = parts[1]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (!string.IsNullOrEmpty(code) && values.Any())
            {
                result[code] = values;
            }
        }

        return result.Any() ? result : null;
    }
}
