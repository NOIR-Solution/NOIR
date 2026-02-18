namespace NOIR.Application.Features.Wishlists.Queries.GetSharedWishlist;

/// <summary>
/// Handler for getting a shared wishlist by share token.
/// </summary>
public sealed class GetSharedWishlistQueryHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;

    public GetSharedWishlistQueryHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<Result<WishlistDetailDto>> Handle(GetSharedWishlistQuery query, CancellationToken ct)
    {
        var spec = new WishlistByShareTokenSpec(query.ShareToken);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("Wishlist", "Shared wishlist not found"));
        }

        return Result.Success(WishlistMapper.ToDetailDto(wishlist));
    }
}
