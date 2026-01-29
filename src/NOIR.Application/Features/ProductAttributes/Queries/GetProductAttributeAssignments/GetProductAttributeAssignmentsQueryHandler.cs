namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeAssignments;

/// <summary>
/// Wolverine handler for getting a product's attribute assignments.
/// </summary>
public class GetProductAttributeAssignmentsQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<Product, Guid> _productRepository;

    public GetProductAttributeAssignmentsQueryHandler(
        IApplicationDbContext dbContext,
        IRepository<Product, Guid> productRepository)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
    }

    public async Task<Result<IReadOnlyCollection<ProductAttributeAssignmentDto>>> Handle(
        GetProductAttributeAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        // Verify product exists
        var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure<IReadOnlyCollection<ProductAttributeAssignmentDto>>(
                Error.NotFound($"Product with ID '{query.ProductId}' not found.", ErrorCodes.Product.NotFound));
        }

        // Query assignments with related attribute data
        var assignmentsQuery = _dbContext.ProductAttributeAssignments
            .Include(pa => pa.Attribute)
            .Where(pa => pa.ProductId == query.ProductId);

        // Filter by variant if specified
        if (query.VariantId.HasValue)
        {
            assignmentsQuery = assignmentsQuery.Where(pa => pa.VariantId == query.VariantId);
        }

        var assignments = await assignmentsQuery
            .OrderBy(pa => pa.Attribute.SortOrder)
            .ThenBy(pa => pa.Attribute.Name)
            .ToListAsync(cancellationToken);

        var dtos = assignments.Select(a => new ProductAttributeAssignmentDto(
            a.Id,
            a.ProductId,
            a.AttributeId,
            a.Attribute.Code,
            a.Attribute.Name,
            a.Attribute.Type.ToString(),
            a.VariantId,
            a.GetTypedValue(),
            a.DisplayValue,
            a.Attribute.IsRequired))
            .ToList();

        return Result.Success<IReadOnlyCollection<ProductAttributeAssignmentDto>>(dtos);
    }
}
