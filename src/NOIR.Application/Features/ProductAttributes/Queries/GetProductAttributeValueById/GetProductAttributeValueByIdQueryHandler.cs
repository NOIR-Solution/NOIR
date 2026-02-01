namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeValueById;

/// <summary>
/// Handler for getting a product attribute value by ID.
/// Uses IApplicationDbContext to query the child entity directly since
/// ProductAttributeValue is not an aggregate root.
/// </summary>
public sealed class GetProductAttributeValueByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetProductAttributeValueByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductAttributeValueDto>> Handle(
        GetProductAttributeValueByIdQuery query,
        CancellationToken ct)
    {
        var value = await _context.ProductAttributeValues
            .AsNoTracking()
            .Where(v => v.Id == query.ValueId)
            .Include(v => v.Attribute)
            .TagWith("GetProductAttributeValueById")
            .FirstOrDefaultAsync(ct);

        if (value is null)
        {
            return Result.Failure<ProductAttributeValueDto>(
                Error.NotFound(
                    $"Product attribute value with ID '{query.ValueId}' not found.",
                    "NOIR-ATTR-VALUE-001"));
        }

        return Result.Success(ProductAttributeMapper.ToValueDto(value));
    }
}
