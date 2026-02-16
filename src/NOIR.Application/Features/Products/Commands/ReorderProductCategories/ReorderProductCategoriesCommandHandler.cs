namespace NOIR.Application.Features.Products.Commands.ReorderProductCategories;

/// <summary>
/// Handler for reordering product categories in bulk.
/// Updates sort order and parent for multiple categories.
/// </summary>
public class ReorderProductCategoriesCommandHandler
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderProductCategoriesCommandHandler(
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<ProductCategoryListDto>>> Handle(
        ReorderProductCategoriesCommand command,
        CancellationToken cancellationToken)
    {
        var requestedIds = command.Items.Select(i => i.CategoryId).ToList();

        // Load all categories with tracking
        var spec = new ProductCategoriesByIdsForUpdateSpec(requestedIds);
        var categories = await _categoryRepository.ListAsync(spec, cancellationToken);

        // Validate all category IDs exist
        var foundIds = categories.Select(c => c.Id).ToHashSet();
        var invalidIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();

        if (invalidIds.Count > 0)
        {
            return Result.Failure<List<ProductCategoryListDto>>(
                Error.Validation(
                    "categoryIds",
                    $"Invalid category IDs: {string.Join(", ", invalidIds)}",
                    "NOIR-CATEGORY-010"));
        }

        // Validate no category is set as its own parent
        foreach (var item in command.Items)
        {
            if (item.ParentId == item.CategoryId)
            {
                return Result.Failure<List<ProductCategoryListDto>>(
                    Error.Validation(
                        "parentId",
                        $"Category '{item.CategoryId}' cannot be its own parent.",
                        "NOIR-CATEGORY-011"));
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
        var allCategoriesSpec = new ProductCategoriesSpec();
        var allCategories = await _categoryRepository.ListAsync(allCategoriesSpec, cancellationToken);
        var categoryLookup = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var childCountLookup = allCategories
            .Where(c => c.ParentId.HasValue)
            .GroupBy(c => c.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = allCategories.Select(c => new ProductCategoryListDto(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.SortOrder,
            c.ProductCount,
            c.ParentId,
            c.ParentId.HasValue && categoryLookup.TryGetValue(c.ParentId.Value, out var parentName)
                ? parentName
                : null,
            childCountLookup.TryGetValue(c.Id, out var childCount) ? childCount : 0
        )).ToList();

        return Result.Success(result);
    }
}
