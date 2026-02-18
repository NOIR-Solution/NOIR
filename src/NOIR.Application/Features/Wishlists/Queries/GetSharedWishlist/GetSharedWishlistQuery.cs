namespace NOIR.Application.Features.Wishlists.Queries.GetSharedWishlist;

/// <summary>
/// Query to get a shared wishlist by share token (public access).
/// </summary>
public sealed record GetSharedWishlistQuery(string ShareToken);
