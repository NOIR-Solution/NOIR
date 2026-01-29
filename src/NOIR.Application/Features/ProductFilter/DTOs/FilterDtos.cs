namespace NOIR.Application.Features.ProductFilter.DTOs;

/// <summary>
/// Request parameters for filtering products.
/// </summary>
public sealed record ProductFilterRequest
{
    public string? CategorySlug { get; init; }
    public List<string>? Brands { get; init; }
    public string? SearchQuery { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public bool InStockOnly { get; init; }
    public Dictionary<string, List<string>> AttributeFilters { get; init; } = new();
    public string Sort { get; init; } = "newest";
    public SortOrder Order { get; init; } = SortOrder.Descending;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}

/// <summary>
/// Sort order direction.
/// </summary>
public enum SortOrder
{
    Ascending,
    Descending
}

/// <summary>
/// Result of a product filter query with facets.
/// </summary>
public sealed record FilteredProductsResult
{
    public List<FilteredProductDto> Products { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public FacetsDto Facets { get; init; } = new();
    public Dictionary<string, List<string>> AppliedFilters { get; init; } = new();
}

/// <summary>
/// Product data for filter results - optimized for list display.
/// </summary>
public sealed record FilteredProductDto(
    Guid Id,
    string Name,
    string Slug,
    ProductStatus Status,
    decimal MinPrice,
    decimal MaxPrice,
    string Currency,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    bool InStock,
    int TotalStock,
    decimal? AverageRating,
    int ReviewCount,
    string? PrimaryImageUrl);

/// <summary>
/// Facets (filter options with counts) for the current result set.
/// </summary>
public sealed record FacetsDto
{
    public List<FacetGroupDto> Brands { get; init; } = new();
    public List<FacetGroupDto> Attributes { get; init; } = new();
    public PriceRangeFacetDto? Price { get; init; }
}

/// <summary>
/// A group of facet values for a single filter type.
/// </summary>
public sealed record FacetGroupDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public FacetDisplayType DisplayType { get; init; }
    public string? Unit { get; init; }
    public List<FacetValueDto> Values { get; init; } = new();
}

/// <summary>
/// How to display a facet in the UI.
/// </summary>
public enum FacetDisplayType
{
    Checkbox,
    Color,
    Range,
    Boolean
}

/// <summary>
/// A single facet value with count.
/// </summary>
public sealed record FacetValueDto(
    string Value,
    string Label,
    int Count,
    bool IsSelected,
    string? ColorCode = null,
    string? SwatchUrl = null);

/// <summary>
/// Price range facet showing min/max available prices.
/// </summary>
public sealed record PriceRangeFacetDto(
    decimal Min,
    decimal Max,
    decimal? SelectedMin,
    decimal? SelectedMax);

/// <summary>
/// Available filters for a category.
/// </summary>
public sealed record CategoryFiltersDto
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public List<FilterDefinitionDto> Filters { get; init; } = new();
}

/// <summary>
/// Definition of a single filter type available for a category.
/// </summary>
public sealed record FilterDefinitionDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public FacetDisplayType DisplayType { get; init; }
    public string? Unit { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public List<FilterValueDto> Values { get; init; } = new();
}

/// <summary>
/// A predefined value for a filter.
/// </summary>
public sealed record FilterValueDto(
    string Value,
    string Label,
    string? ColorCode = null,
    string? SwatchUrl = null,
    int ProductCount = 0);
