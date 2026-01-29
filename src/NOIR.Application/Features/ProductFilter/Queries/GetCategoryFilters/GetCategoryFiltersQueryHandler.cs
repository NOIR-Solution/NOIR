using NOIR.Application.Features.ProductFilter.DTOs;

namespace NOIR.Application.Features.ProductFilter.Queries.GetCategoryFilters;

/// <summary>
/// Handler for getting available filters for a category.
/// Returns filter definitions based on category attributes.
/// </summary>
public class GetCategoryFiltersQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<GetCategoryFiltersQueryHandler> _logger;

    public GetCategoryFiltersQueryHandler(
        IApplicationDbContext dbContext,
        ILogger<GetCategoryFiltersQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CategoryFiltersDto>> Handle(
        GetCategoryFiltersQuery query,
        CancellationToken ct)
    {
        _logger.LogDebug("Getting filters for category: {CategorySlug}", query.CategorySlug);

        // Get the category
        var category = await _dbContext.ProductCategories
            .TagWith("GetCategoryFilters.GetCategory")
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == query.CategorySlug, ct);

        if (category == null)
        {
            return Result.Failure<CategoryFiltersDto>(
                Error.NotFound($"Category '{query.CategorySlug}' not found.", ErrorCodes.Product.CategoryNotFound));
        }

        var filters = new List<FilterDefinitionDto>();

        // Add brand filter
        var brands = await GetBrandsForCategoryAsync(category.Id, ct);
        if (brands.Any())
        {
            filters.Add(new FilterDefinitionDto
            {
                Code = "brand",
                Name = "Brand",
                Type = "select",
                DisplayType = FacetDisplayType.Checkbox,
                Values = brands
            });
        }

        // Add price filter
        var priceRange = await GetPriceRangeForCategoryAsync(category.Id, ct);
        if (priceRange != null)
        {
            filters.Add(new FilterDefinitionDto
            {
                Code = "price",
                Name = "Price",
                Type = "range",
                DisplayType = FacetDisplayType.Range,
                Min = priceRange.Value.min,
                Max = priceRange.Value.max
            });
        }

        // Add in-stock filter
        filters.Add(new FilterDefinitionDto
        {
            Code = "in_stock",
            Name = "Availability",
            Type = "boolean",
            DisplayType = FacetDisplayType.Boolean,
            Values = new List<FilterValueDto>
            {
                new("true", "In Stock"),
                new("false", "Out of Stock")
            }
        });

        // Get category-specific attributes
        var categoryAttributes = await GetCategoryAttributesAsync(category.Id, ct);
        filters.AddRange(categoryAttributes);

        var result = new CategoryFiltersDto
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            CategorySlug = category.Slug,
            Filters = filters
        };

        return Result.Success(result);
    }

    private async Task<List<FilterValueDto>> GetBrandsForCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var categoryIdStr = categoryId.ToString();

        return await _dbContext.ProductFilterIndexes
            .TagWith("GetCategoryFilters.GetBrands")
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active &&
                       (p.CategoryId == categoryId ||
                        (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr))))
            .Where(p => p.BrandId != null && p.BrandSlug != null)
            .GroupBy(p => new { p.BrandId, p.BrandName, p.BrandSlug })
            .Select(g => new FilterValueDto(
                g.Key.BrandSlug!,
                g.Key.BrandName ?? g.Key.BrandSlug!,
                null,
                null,
                g.Count()))
            .OrderByDescending(b => b.ProductCount)
            .Take(50)
            .ToListAsync(ct);
    }

    private async Task<(decimal min, decimal max)?> GetPriceRangeForCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        var categoryIdStr = categoryId.ToString();

        var priceStats = await _dbContext.ProductFilterIndexes
            .TagWith("GetCategoryFilters.GetPriceRange")
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active &&
                       (p.CategoryId == categoryId ||
                        (p.CategoryPath != null && p.CategoryPath.StartsWith(categoryIdStr))))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Min = g.Min(p => p.MinPrice),
                Max = g.Max(p => p.MaxPrice)
            })
            .FirstOrDefaultAsync(ct);

        if (priceStats == null)
            return null;

        return (priceStats.Min, priceStats.Max);
    }

    private async Task<List<FilterDefinitionDto>> GetCategoryAttributesAsync(Guid categoryId, CancellationToken ct)
    {
        var filters = new List<FilterDefinitionDto>();

        // Get attributes assigned to this category (including inherited from parents)
        var categoryAttributes = await _dbContext.CategoryAttributes
            .TagWith("GetCategoryFilters.GetCategoryAttributes")
            .AsNoTracking()
            .Where(ca => ca.CategoryId == categoryId)
            .Include(ca => ca.Attribute)
            .ThenInclude(a => a.Values.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .Where(ca => ca.Attribute.IsFilterable && ca.Attribute.IsActive)
            .OrderBy(ca => ca.SortOrder)
            .ToListAsync(ct);

        // If no category-specific attributes, get global filterable attributes
        if (!categoryAttributes.Any())
        {
            var globalAttributes = await _dbContext.ProductAttributes
                .TagWith("GetCategoryFilters.GetGlobalAttributes")
                .AsNoTracking()
                .Where(a => a.IsFilterable && a.IsActive)
                .Include(a => a.Values.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
                .OrderBy(a => a.SortOrder)
                .ToListAsync(ct);

            foreach (var attr in globalAttributes)
            {
                filters.Add(CreateFilterDefinition(attr));
            }
        }
        else
        {
            foreach (var ca in categoryAttributes)
            {
                filters.Add(CreateFilterDefinition(ca.Attribute));
            }
        }

        return filters;
    }

    private static FilterDefinitionDto CreateFilterDefinition(ProductAttribute attr)
    {
        var displayType = attr.Type switch
        {
            AttributeType.Color => FacetDisplayType.Color,
            AttributeType.Boolean => FacetDisplayType.Boolean,
            AttributeType.Number or AttributeType.Decimal or AttributeType.Range => FacetDisplayType.Range,
            _ => FacetDisplayType.Checkbox
        };

        var values = attr.Values
            .Select(v => new FilterValueDto(
                v.Value,
                v.DisplayValue,
                v.ColorCode,
                v.SwatchUrl,
                v.ProductCount))
            .ToList();

        return new FilterDefinitionDto
        {
            Code = attr.Code,
            Name = attr.Name,
            Type = attr.Type.ToString().ToLower(),
            DisplayType = displayType,
            Unit = attr.Unit,
            Min = attr.MinValue,
            Max = attr.MaxValue,
            Values = values
        };
    }
}
