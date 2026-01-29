namespace NOIR.Application.Features.Products.Queries.GetProductOptionById;

/// <summary>
/// Handler for getting a product option by ID.
/// Navigates through Product aggregate to find the option.
/// </summary>
public class GetProductOptionByIdQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;

    public GetProductOptionByIdQueryHandler(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductOptionDto>> Handle(
        GetProductOptionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ProductByOptionIdSpec(query.OptionId);
        var product = await _productRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductOptionDto>(
                Error.NotFound("Product option not found.", "NOIR-PRODUCT-051"));
        }

        var option = product.Options.FirstOrDefault(o => o.Id == query.OptionId);
        if (option is null)
        {
            return Result.Failure<ProductOptionDto>(
                Error.NotFound("Product option not found.", "NOIR-PRODUCT-051"));
        }

        return Result.Success(ProductMapper.ToDto(option));
    }
}
