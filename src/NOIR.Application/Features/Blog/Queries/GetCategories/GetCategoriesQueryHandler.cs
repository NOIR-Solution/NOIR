using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

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
            c.Children?.Count ?? 0
        )).ToList();

        return Result.Success(result);
    }
}
