namespace NOIR.Application.Features.Products.Queries.GetProductById;

/// <summary>
/// Wolverine handler for getting a single product.
/// </summary>
public class GetProductByIdQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly ICurrentUser _currentUser;

    public GetProductByIdQueryHandler(
        IRepository<Product, Guid> productRepository,
        ICurrentUser currentUser)
    {
        _productRepository = productRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        Product? product = null;

        if (query.Id.HasValue)
        {
            var spec = new ProductByIdSpec(query.Id.Value);
            product = await _productRepository.FirstOrDefaultAsync(spec, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(query.Slug))
        {
            var tenantId = _currentUser.TenantId;
            var spec = new ProductBySlugSpec(query.Slug, tenantId);
            product = await _productRepository.FirstOrDefaultAsync(spec, cancellationToken);
        }
        else
        {
            return Result.Failure<ProductDto>(
                Error.Validation("Id", "Either ID or Slug must be provided.", "NOIR-PRODUCT-014"));
        }

        if (product is null)
        {
            return Result.Failure<ProductDto>(
                Error.NotFound("Product not found.", "NOIR-PRODUCT-012"));
        }

        return Result.Success(ProductMapper.ToDto(product));
    }
}
