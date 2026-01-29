using NOIR.Application.Features.ProductFilter.DTOs;

namespace NOIR.Application.Features.ProductFilter.Queries.FilterProducts;

/// <summary>
/// Query to filter products using the denormalized ProductFilterIndex.
/// Supports faceted filtering with real-time counts.
/// </summary>
public sealed record FilterProductsQuery(
    string? CategorySlug = null,
    List<string>? Brands = null,
    string? SearchQuery = null,
    decimal? PriceMin = null,
    decimal? PriceMax = null,
    bool InStockOnly = false,
    Dictionary<string, List<string>>? AttributeFilters = null,
    string Sort = "newest",
    SortOrder Order = SortOrder.Descending,
    int Page = 1,
    int PageSize = 24);
