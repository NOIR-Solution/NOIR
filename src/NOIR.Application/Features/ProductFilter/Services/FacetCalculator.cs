using System.Text.Json;
using NOIR.Application.Features.ProductFilter.DTOs;
using ProductFilterIndexEntity = NOIR.Domain.Entities.Product.ProductFilterIndex;

namespace NOIR.Application.Features.ProductFilter.Services;

/// <summary>
/// Calculates facets (filter options with counts) for product filtering.
/// Uses the ProductFilterIndex for efficient counting.
/// </summary>
public class FacetCalculator : IScopedService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<FacetCalculator> _logger;

    public FacetCalculator(
        IApplicationDbContext dbContext,
        ILogger<FacetCalculator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Calculates facets for the given filtered query.
    /// </summary>
    public async Task<FacetsDto> CalculateFacetsAsync(
        IQueryable<ProductFilterIndexEntity> filteredQuery,
        Dictionary<string, List<string>> appliedFilters,
        decimal? priceMin,
        decimal? priceMax,
        CancellationToken ct)
    {
        _logger.LogDebug("Calculating facets for filtered query");

        // Calculate brand facets
        var brandFacets = await CalculateBrandFacetsAsync(filteredQuery, appliedFilters, ct);

        // Calculate price range
        var priceFacet = await CalculatePriceRangeFacetAsync(filteredQuery, priceMin, priceMax, ct);

        // Calculate attribute facets
        var attributeFacets = await CalculateAttributeFacetsAsync(filteredQuery, appliedFilters, ct);

        return new FacetsDto
        {
            Brands = brandFacets,
            Attributes = attributeFacets,
            Price = priceFacet
        };
    }

    private async Task<List<FacetGroupDto>> CalculateBrandFacetsAsync(
        IQueryable<ProductFilterIndexEntity> query,
        Dictionary<string, List<string>> appliedFilters,
        CancellationToken ct)
    {
        var selectedBrands = appliedFilters.GetValueOrDefault("brand") ?? new List<string>();

        var brandCounts = await query
            .TagWith("FacetCalculator.BrandCounts")
            .Where(p => p.BrandId != null && p.BrandSlug != null)
            .GroupBy(p => new { p.BrandId, p.BrandName, p.BrandSlug })
            .Select(g => new
            {
                g.Key.BrandSlug,
                g.Key.BrandName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(50)
            .ToListAsync(ct);

        if (!brandCounts.Any())
            return new List<FacetGroupDto>();

        var brandGroup = new FacetGroupDto
        {
            Code = "brand",
            Name = "Brand",
            DisplayType = FacetDisplayType.Checkbox,
            Values = brandCounts.Select(b => new FacetValueDto(
                b.BrandSlug!,
                b.BrandName ?? b.BrandSlug!,
                b.Count,
                selectedBrands.Contains(b.BrandSlug!))).ToList()
        };

        return new List<FacetGroupDto> { brandGroup };
    }

    private async Task<PriceRangeFacetDto?> CalculatePriceRangeFacetAsync(
        IQueryable<ProductFilterIndexEntity> query,
        decimal? selectedMin,
        decimal? selectedMax,
        CancellationToken ct)
    {
        var priceStats = await query
            .TagWith("FacetCalculator.PriceRange")
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Min = g.Min(p => p.MinPrice),
                Max = g.Max(p => p.MaxPrice)
            })
            .FirstOrDefaultAsync(ct);

        if (priceStats == null)
            return null;

        return new PriceRangeFacetDto(
            priceStats.Min,
            priceStats.Max,
            selectedMin,
            selectedMax);
    }

    private async Task<List<FacetGroupDto>> CalculateAttributeFacetsAsync(
        IQueryable<ProductFilterIndexEntity> query,
        Dictionary<string, List<string>> appliedFilters,
        CancellationToken ct)
    {
        var facetGroups = new List<FacetGroupDto>();

        // Get filterable attributes
        var filterableAttributes = await _dbContext.ProductAttributes
            .TagWith("FacetCalculator.GetFilterableAttributes")
            .AsNoTracking()
            .Where(a => a.IsFilterable && a.IsActive)
            .Include(a => a.Values.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        // Get all AttributesJson values for analysis
        var attributesJsonList = await query
            .TagWith("FacetCalculator.GetAttributesJson")
            .Select(p => p.AttributesJson)
            .Where(json => json != "{}")
            .Take(1000) // Limit for performance
            .ToListAsync(ct);

        foreach (var attr in filterableAttributes)
        {
            var selectedValues = appliedFilters.GetValueOrDefault(attr.Code) ?? new List<string>();

            // For Select/MultiSelect attributes with predefined values
            if (attr.Type == AttributeType.Select || attr.Type == AttributeType.MultiSelect)
            {
                var valueCounts = CalculateAttributeValueCounts(attributesJsonList, attr.Code);

                var values = attr.Values
                    .Where(v => valueCounts.ContainsKey(v.DisplayValue))
                    .Select(v => new FacetValueDto(
                        v.Value,
                        v.DisplayValue,
                        valueCounts.GetValueOrDefault(v.DisplayValue, 0),
                        selectedValues.Contains(v.Value),
                        v.ColorCode,
                        v.SwatchUrl))
                    .Where(v => v.Count > 0)
                    .ToList();

                if (values.Any())
                {
                    facetGroups.Add(new FacetGroupDto
                    {
                        Code = attr.Code,
                        Name = attr.Name,
                        DisplayType = attr.Type == AttributeType.Color
                            ? FacetDisplayType.Color
                            : FacetDisplayType.Checkbox,
                        Unit = attr.Unit,
                        Values = values
                    });
                }
            }
            else if (attr.Type == AttributeType.Boolean)
            {
                var (trueCount, falseCount) = CalculateBooleanAttributeCounts(attributesJsonList, attr.Code);

                if (trueCount > 0 || falseCount > 0)
                {
                    facetGroups.Add(new FacetGroupDto
                    {
                        Code = attr.Code,
                        Name = attr.Name,
                        DisplayType = FacetDisplayType.Boolean,
                        Values = new List<FacetValueDto>
                        {
                            new("true", "Yes", trueCount, selectedValues.Contains("true")),
                            new("false", "No", falseCount, selectedValues.Contains("false"))
                        }
                    });
                }
            }
            else if (attr.Type == AttributeType.Color)
            {
                var colorCounts = CalculateAttributeValueCounts(attributesJsonList, attr.Code);

                var values = attr.Values
                    .Where(v => colorCounts.ContainsKey(v.DisplayValue))
                    .Select(v => new FacetValueDto(
                        v.Value,
                        v.DisplayValue,
                        colorCounts.GetValueOrDefault(v.DisplayValue, 0),
                        selectedValues.Contains(v.Value),
                        v.ColorCode,
                        v.SwatchUrl))
                    .Where(v => v.Count > 0)
                    .ToList();

                if (values.Any())
                {
                    facetGroups.Add(new FacetGroupDto
                    {
                        Code = attr.Code,
                        Name = attr.Name,
                        DisplayType = FacetDisplayType.Color,
                        Values = values
                    });
                }
            }
            // For numeric attributes (Number, Decimal, Range), we could add range facets
            // but this is more complex and often done differently
        }

        return facetGroups;
    }

    private static Dictionary<string, int> CalculateAttributeValueCounts(
        List<string> attributesJsonList,
        string attributeCode)
    {
        var counts = new Dictionary<string, int>();

        foreach (var json in attributesJsonList)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(attributeCode, out var value))
                {
                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in value.EnumerateArray())
                        {
                            var strValue = item.GetString();
                            if (!string.IsNullOrEmpty(strValue))
                            {
                                counts[strValue] = counts.GetValueOrDefault(strValue, 0) + 1;
                            }
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        var strValue = value.GetString();
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            counts[strValue] = counts.GetValueOrDefault(strValue, 0) + 1;
                        }
                    }
                }
            }
            catch
            {
                // Skip malformed JSON
            }
        }

        return counts;
    }

    private static (int trueCount, int falseCount) CalculateBooleanAttributeCounts(
        List<string> attributesJsonList,
        string attributeCode)
    {
        var trueCount = 0;
        var falseCount = 0;

        foreach (var json in attributesJsonList)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(attributeCode, out var value))
                {
                    if (value.ValueKind == JsonValueKind.True)
                        trueCount++;
                    else if (value.ValueKind == JsonValueKind.False)
                        falseCount++;
                }
            }
            catch
            {
                // Skip malformed JSON
            }
        }

        return (trueCount, falseCount);
    }
}
