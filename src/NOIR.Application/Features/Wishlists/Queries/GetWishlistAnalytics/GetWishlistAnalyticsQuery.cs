namespace NOIR.Application.Features.Wishlists.Queries.GetWishlistAnalytics;

/// <summary>
/// Query to get wishlist analytics (admin: top wishlisted products).
/// </summary>
public sealed record GetWishlistAnalyticsQuery(int TopCount = 10);
