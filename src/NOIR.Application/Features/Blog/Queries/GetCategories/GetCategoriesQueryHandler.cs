
namespace NOIR.Application.Features.Blog.Queries.GetCategories;

/// <summary>
/// Wolverine handler for getting a list of blog categories.
/// </summary>
public class GetCategoriesQueryHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;

    public GetCategoriesQueryHandler(IRepository<PostCategory, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<List<PostCategoryListDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PostCategory> categoriesResult;

        if (query.TopLevelOnly)
        {
            var spec = new TopLevelCategoriesSpec();
            categoriesResult = await _categoryRepository.ListAsync(spec, cancellationToken);
        }
        else
        {
            var spec = new CategoriesSpec(query.Search, query.IncludeChildren);
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

        var result = categories.Select(c => new PostCategoryListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.SortOrder,
            c.PostCount,
            c.ParentId,
            c.ParentId.HasValue && categoryDict.TryGetValue(c.ParentId.Value, out var parentName)
                ? parentName
                : c.Parent?.Name,
            childCountLookup.TryGetValue(c.Id, out var childCount) ? childCount : 0
        )).ToList();

        return Result.Success(result);
    }
}
