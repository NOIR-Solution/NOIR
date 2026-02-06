namespace NOIR.Application.Features.Products.Queries.GetProductCategories;

/// <summary>
/// Wolverine handler for getting a list of product categories.
/// </summary>
public class GetProductCategoriesQueryHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;

    public GetProductCategoriesQueryHandler(IRepository<ProductCategory, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<List<ProductCategoryListDto>>> Handle(
        GetProductCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductCategory> categoriesResult;

        if (query.TopLevelOnly)
        {
            var spec = new TopLevelProductCategoriesSpec();
            categoriesResult = await _categoryRepository.ListAsync(spec, cancellationToken);
        }
        else
        {
            var spec = new ProductCategoriesSpec(query.Search, query.IncludeChildren);
            categoriesResult = await _categoryRepository.ListAsync(spec, cancellationToken);
        }

        var categories = categoriesResult.ToList();

        // Build a lookup for parent names
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        // Build parent → child count lookup (O(n) instead of O(n²))
        var childCountLookup = categories
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = categories.Select(c => new ProductCategoryListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.SortOrder,
            c.ProductCount,
            c.ParentId,
            c.ParentId.HasValue && categoryDict.TryGetValue(c.ParentId.Value, out var parentName)
                ? parentName
                : c.Parent?.Name,
            childCountLookup.TryGetValue(c.Id, out var childCount) ? childCount : 0
        )).ToList();

        return Result.Success(result);
    }
}
