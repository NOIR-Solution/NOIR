namespace NOIR.Application.Features.Wishlists.Queries.GetWishlists;

/// <summary>
/// Handler for getting all wishlists for a user.
/// </summary>
public sealed class GetWishlistsQueryHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;

    public GetWishlistsQueryHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<Result<List<WishlistDto>>> Handle(GetWishlistsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(query.UserId))
        {
            return Result.Failure<List<WishlistDto>>(Error.Validation("UserId", "User must be authenticated"));
        }

        var spec = new WishlistsByUserSpec(query.UserId);
        var wishlists = await _wishlistRepository.ListAsync(spec, ct);

        var dtos = wishlists.Select(WishlistMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
