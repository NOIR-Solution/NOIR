
namespace NOIR.Application.Features.Blog.Queries.GetCategoryById;

/// <summary>
/// Wolverine handler for getting a single blog category by ID.
/// </summary>
public class GetCategoryByIdQueryHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;

    public GetCategoryByIdQueryHandler(IRepository<PostCategory, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<PostCategoryDto>> Handle(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new CategoryByIdSpec(query.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<PostCategoryDto>(
                Error.NotFound("Category not found.", "NOIR-BLOG-020"));
        }

        return Result.Success(MapToDto(category));
    }

    private static PostCategoryDto MapToDto(PostCategory category)
    {
        var children = category.Children?
            .Select(c => new PostCategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.MetaTitle,
                c.MetaDescription,
                c.ImageUrl,
                c.SortOrder,
                c.PostCount,
                c.ParentId,
                null, // ParentName not needed for children
                null, // Children not recursively loaded
                c.CreatedAt,
                c.ModifiedAt))
            .ToList();

        return new PostCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MetaTitle,
            category.MetaDescription,
            category.ImageUrl,
            category.SortOrder,
            category.PostCount,
            category.ParentId,
            category.Parent?.Name,
            children,
            category.CreatedAt,
            category.ModifiedAt);
    }
}
