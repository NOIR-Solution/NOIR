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
            c.Children?.Count ?? 0
        )).ToList();

        return Result.Success(result);
    }
}
