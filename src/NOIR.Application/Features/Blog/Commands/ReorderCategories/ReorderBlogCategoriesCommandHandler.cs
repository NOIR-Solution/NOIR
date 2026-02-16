namespace NOIR.Application.Features.Blog.Commands.ReorderCategories;

/// <summary>
/// Handler for reordering blog categories in bulk.
/// Updates sort order and parent for multiple categories.
/// </summary>
public class ReorderBlogCategoriesCommandHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderBlogCategoriesCommandHandler(
        IRepository<PostCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<PostCategoryListDto>>> Handle(
        ReorderBlogCategoriesCommand command,
        CancellationToken cancellationToken)
    {
        var requestedIds = command.Items.Select(i => i.CategoryId).ToList();

        // Load all categories with tracking
        var spec = new CategoriesByIdsForUpdateSpec(requestedIds);
        var categories = await _categoryRepository.ListAsync(spec, cancellationToken);

        // Validate all category IDs exist
        var foundIds = categories.Select(c => c.Id).ToHashSet();
        var invalidIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();

        if (invalidIds.Count > 0)
        {
            return Result.Failure<List<PostCategoryListDto>>(
                Error.Validation(
                    "categoryIds",
                    $"Invalid category IDs: {string.Join(", ", invalidIds)}",
                    "NOIR-BLOG-CATEGORY-010"));
        }

        // Validate no category is set as its own parent
        foreach (var item in command.Items)
        {
            if (item.ParentId == item.CategoryId)
            {
                return Result.Failure<List<PostCategoryListDto>>(
                    Error.Validation(
                        "parentId",
                        $"Category '{item.CategoryId}' cannot be its own parent.",
                        "NOIR-BLOG-CATEGORY-011"));
            }
        }

        // Update sort orders and parents
        var categoryDict = categories.ToDictionary(c => c.Id);
        foreach (var item in command.Items)
        {
            var category = categoryDict[item.CategoryId];
            category.SetSortOrder(item.SortOrder);
            category.SetParent(item.ParentId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return updated list
        var allCategoriesSpec = new CategoriesSpec();
        var allCategories = await _categoryRepository.ListAsync(allCategoriesSpec, cancellationToken);
        var categoryLookup = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var childCountLookup = allCategories
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = allCategories.Select(c => new PostCategoryListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.SortOrder,
            c.PostCount,
            c.ParentId,
            c.ParentId.HasValue && categoryLookup.TryGetValue(c.ParentId.Value, out var parentName)
                ? parentName
                : null,
            childCountLookup.TryGetValue(c.Id, out var childCount) ? childCount : 0
        )).ToList();

        return Result.Success(result);
    }
}
