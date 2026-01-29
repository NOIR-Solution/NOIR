namespace NOIR.Application.Features.Products.Queries.GetProductOptionValueById;

/// <summary>
/// Handler for getting a product option value by ID.
/// Navigates through Product aggregate to find the option value.
/// </summary>
public class GetProductOptionValueByIdQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;

    public GetProductOptionValueByIdQueryHandler(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductOptionValueDto>> Handle(
        GetProductOptionValueByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ProductByOptionValueIdSpec(query.ValueId);
        var product = await _productRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.NotFound("Product option value not found.", "NOIR-PRODUCT-052"));
        }

        var optionValue = product.Options
            .SelectMany(o => o.Values)
            .FirstOrDefault(v => v.Id == query.ValueId);

        if (optionValue is null)
        {
            return Result.Failure<ProductOptionValueDto>(
                Error.NotFound("Product option value not found.", "NOIR-PRODUCT-052"));
        }

        return Result.Success(ProductMapper.ToDto(optionValue));
    }
}
