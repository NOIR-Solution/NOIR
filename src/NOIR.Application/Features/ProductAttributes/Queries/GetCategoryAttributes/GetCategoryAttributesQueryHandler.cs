namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributes;

/// <summary>
/// Wolverine handler for getting attributes assigned to a category.
/// </summary>
public class GetCategoryAttributesQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;

    public GetCategoryAttributesQueryHandler(
        IApplicationDbContext dbContext,
        IRepository<ProductCategory, Guid> categoryRepository)
    {
        _dbContext = dbContext;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IReadOnlyList<CategoryAttributeDto>>> Handle(
        GetCategoryAttributesQuery query,
        CancellationToken cancellationToken)
    {
        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(query.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<IReadOnlyList<CategoryAttributeDto>>(
                Error.NotFound($"Category with ID '{query.CategoryId}' not found.", "NOIR-PRODUCT-003"));
        }

        // Get all category-attribute links for this category
        var categoryAttributes = await _dbContext.CategoryAttributes
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .Where(ca => ca.CategoryId == query.CategoryId)
            .OrderBy(ca => ca.SortOrder)
            .ToListAsync(cancellationToken);

        var dtos = categoryAttributes
            .Select(ProductAttributeMapper.ToCategoryAttributeDto)
            .ToList();

        return Result.Success<IReadOnlyList<CategoryAttributeDto>>(dtos);
    }
}
