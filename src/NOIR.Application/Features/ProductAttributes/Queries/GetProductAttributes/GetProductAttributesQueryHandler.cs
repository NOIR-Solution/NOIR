namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributes;

/// <summary>
/// Wolverine handler for getting paged list of product attributes.
/// </summary>
public class GetProductAttributesQueryHandler
{
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;

    public GetProductAttributesQueryHandler(IRepository<ProductAttribute, Guid> attributeRepository)
    {
        _attributeRepository = attributeRepository;
    }

    public async Task<Result<PagedResult<ProductAttributeListDto>>> Handle(
        GetProductAttributesQuery query,
        CancellationToken cancellationToken)
    {
        // Get paged attributes
        var spec = new ProductAttributesPagedSpec(
            query.Search,
            query.IsActive,
            query.IsFilterable,
            query.IsVariantAttribute,
            query.Type,
            query.Page,
            query.PageSize,
            includeValues: true);

        var attributes = await _attributeRepository.ListAsync(spec, cancellationToken);

        // Get total count
        var countSpec = new ProductAttributesCountSpec(
            query.Search,
            query.IsActive,
            query.IsFilterable,
            query.IsVariantAttribute,
            query.Type);

        var totalCount = await _attributeRepository.CountAsync(countSpec, cancellationToken);

        var items = attributes.Select(ProductAttributeMapper.ToListDto).ToList();

        // PagedResult expects pageIndex (0-based), but query.Page is 1-based
        var pageIndex = query.Page - 1;

        return Result.Success(PagedResult<ProductAttributeListDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
