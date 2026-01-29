namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeById;

/// <summary>
/// Wolverine handler for getting a product attribute by ID.
/// </summary>
public class GetProductAttributeByIdQueryHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;

    public GetProductAttributeByIdQueryHandler(IRepository<ProductAttribute, Guid> attributeRepository)
    {
        _attributeRepository = attributeRepository;
    }

    public async Task<Result<ProductAttributeDto>> Handle(
        GetProductAttributeByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ProductAttributeByIdSpec(query.Id, includeValues: true);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<ProductAttributeDto>(
                Error.NotFound($"Product attribute with ID '{query.Id}' was not found.", ErrorCodes.Attribute.NotFound));
        }

        return Result.Success(ProductAttributeMapper.ToDto(attribute));
    }
}
