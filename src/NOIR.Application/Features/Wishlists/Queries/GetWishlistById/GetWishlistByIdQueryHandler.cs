namespace NOIR.Application.Features.Wishlists.Queries.GetWishlistById;

/// <summary>
/// Handler for getting a wishlist by ID with item details.
/// </summary>
public sealed class GetWishlistByIdQueryHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;

    public GetWishlistByIdQueryHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<Result<WishlistDetailDto>> Handle(GetWishlistByIdQuery query, CancellationToken ct)
    {
        var spec = new WishlistDetailByIdSpec(query.Id);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("Wishlist", query.Id));
        }

        if (wishlist.UserId != query.UserId)
        {
            return Result.Failure<WishlistDetailDto>(Error.Validation("Wishlist", "You can only view your own wishlists"));
        }

        return Result.Success(WishlistMapper.ToDetailDto(wishlist));
    }
}
