namespace NOIR.Application.Features.Products.Queries.GetProductStats;

/// <summary>
/// Wolverine handler for product statistics query.
/// Uses parallel database counts for optimal performance.
/// </summary>
public class GetProductStatsQueryHandler
{
    private readonly IReadRepository<Product, Guid> _repository;

    public GetProductStatsQueryHandler(IReadRepository<Product, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductStatsDto>> Handle(
        GetProductStatsQuery query,
        CancellationToken cancellationToken)
    {
        // Execute count queries sequentially - DbContext is NOT thread-safe
        // so parallel queries cause "A second operation was started" errors
        var total = await _repository.CountAsync(cancellationToken);
        var active = await _repository.CountAsync(new ProductsByStatusCountSpec(ProductStatus.Active), cancellationToken);
        var draft = await _repository.CountAsync(new ProductsByStatusCountSpec(ProductStatus.Draft), cancellationToken);
        var archived = await _repository.CountAsync(new ProductsByStatusCountSpec(ProductStatus.Archived), cancellationToken);
        var outOfStock = await _repository.CountAsync(new ProductsByStatusCountSpec(ProductStatus.OutOfStock), cancellationToken);
        var lowStock = await _repository.CountAsync(new ProductsLowStockCountSpec(ProductConstants.LowStockThreshold), cancellationToken);

        var stats = new ProductStatsDto(
            Total: total,
            Active: active,
            Draft: draft,
            Archived: archived,
            OutOfStock: outOfStock,
            LowStock: lowStock
        );

        return Result.Success(stats);
    }
}
