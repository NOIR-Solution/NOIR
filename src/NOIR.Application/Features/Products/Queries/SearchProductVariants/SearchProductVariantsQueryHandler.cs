namespace NOIR.Application.Features.Products.Queries.SearchProductVariants;

/// <summary>
/// Wolverine handler for searching product variants.
/// Queries through Product aggregate and flattens variants into lookup DTOs.
/// </summary>
public class SearchProductVariantsQueryHandler
{
    private readonly IRepository<Product, Guid> _productRepository;

    public SearchProductVariantsQueryHandler(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<GetProducts.PagedResult<ProductVariantLookupDto>>> Handle(
        SearchProductVariantsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new SearchProductsWithVariantsSpec(
            query.Search,
            query.CategoryId,
            skip,
            query.PageSize);

        var products = await _productRepository.ListAsync(spec, cancellationToken);

        var countSpec = new SearchProductsWithVariantsCountSpec(
            query.Search,
            query.CategoryId);

        var totalCount = await _productRepository.CountAsync(countSpec, cancellationToken);

        // Flatten products into variant-level DTOs
        var items = products
            .SelectMany(p => p.Variants.Select(v =>
            {
                var primaryImage = p.Images.FirstOrDefault(i => i.IsPrimary)
                    ?? p.Images.FirstOrDefault();

                return new ProductVariantLookupDto(
                    v.Id,
                    p.Id,
                    p.Name,
                    v.Name,
                    v.Sku,
                    v.Price,
                    v.StockQuantity,
                    primaryImage?.Url);
            }))
            .ToList();

        var result = new GetProducts.PagedResult<ProductVariantLookupDto>(
            items,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }
}
