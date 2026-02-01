namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeById;

/// <summary>
/// Handler for getting a category-attribute link by ID.
/// Uses IApplicationDbContext to query the junction entity directly since
/// CategoryAttribute is not an aggregate root.
/// </summary>
public sealed class GetCategoryAttributeByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetCategoryAttributeByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CategoryAttributeDto>> Handle(
        GetCategoryAttributeByIdQuery query,
        CancellationToken ct)
    {
        var categoryAttribute = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(ca => ca.Id == query.Id)
            .Include(ca => ca.Category)
            .Include(ca => ca.Attribute)
            .TagWith("GetCategoryAttributeById")
            .FirstOrDefaultAsync(ct);

        if (categoryAttribute is null)
        {
            return Result.Failure<CategoryAttributeDto>(
                Error.NotFound(
                    $"Category attribute with ID '{query.Id}' not found.",
                    "NOIR-CAT-ATTR-001"));
        }

        return Result.Success(ProductAttributeMapper.ToCategoryAttributeDto(categoryAttribute));
    }
}
