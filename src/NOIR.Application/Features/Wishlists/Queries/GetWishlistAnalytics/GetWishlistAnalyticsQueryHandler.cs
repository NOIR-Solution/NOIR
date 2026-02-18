namespace NOIR.Application.Features.Wishlists.Queries.GetWishlistAnalytics;

/// <summary>
/// Handler for getting wishlist analytics.
/// </summary>
public sealed class GetWishlistAnalyticsQueryHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;

    public GetWishlistAnalyticsQueryHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<Result<WishlistAnalyticsDto>> Handle(GetWishlistAnalyticsQuery query, CancellationToken ct)
    {
        // Get all wishlists with items and product info
        var allSpec = new AllWishlistsWithItemsSpec();
        var wishlists = await _wishlistRepository.ListAsync(allSpec, ct);

        var totalWishlists = wishlists.Count;
        var allItems = wishlists.SelectMany(w => w.Items).ToList();
        var totalItems = allItems.Count;

        // Top wishlisted products
        var topProducts = allItems
            .GroupBy(i => i.ProductId)
            .OrderByDescending(g => g.Count())
            .Take(query.TopCount)
            .Select(g =>
            {
                var firstItem = g.First();
                return new TopWishlistedProductDto
                {
                    ProductId = g.Key,
                    ProductName = firstItem.Product?.Name ?? "Unknown",
                    ProductImage = firstItem.Product?.PrimaryImage?.Url,
                    WishlistCount = g.Count()
                };
            })
            .ToList();

        return Result.Success(new WishlistAnalyticsDto
        {
            TotalWishlists = totalWishlists,
            TotalWishlistItems = totalItems,
            TopProducts = topProducts
        });
    }
}
