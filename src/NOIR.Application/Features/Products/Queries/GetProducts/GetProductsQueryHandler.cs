namespace NOIR.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Wolverine handler for getting a list of products.
/// </summary>
public class GetProductsQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;

    public GetProductsQueryHandler(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductListDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get products with pagination
        var spec = new ProductsSpec(
            query.Search,
            query.Status,
            query.CategoryId,
            query.Brand,
            query.MinPrice,
            query.MaxPrice,
            query.InStockOnly,
            skip,
            query.PageSize);

        var products = await _productRepository.ListAsync(spec, cancellationToken);

        // Get total count for pagination (without skip/take)
        var countSpec = new ProductsCountSpec(
            query.Search,
            query.Status,
            query.CategoryId,
            query.Brand,
            query.MinPrice,
            query.MaxPrice);

        var totalCount = await _productRepository.CountAsync(countSpec, cancellationToken);

        var items = products.Select(ProductMapper.ToListDto).ToList();

        var result = new PagedResult<ProductListDto>(
            items,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}

/// <summary>
/// Paged result for list queries.
/// </summary>
public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
