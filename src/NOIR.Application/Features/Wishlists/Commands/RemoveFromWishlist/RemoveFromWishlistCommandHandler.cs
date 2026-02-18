namespace NOIR.Application.Features.Wishlists.Commands.RemoveFromWishlist;

/// <summary>
/// Handler for removing an item from a wishlist.
/// </summary>
public sealed class RemoveFromWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDetailDto>> Handle(RemoveFromWishlistCommand command, CancellationToken ct)
    {
        var spec = new WishlistItemByIdSpec(command.WishlistItemId);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("WishlistItem", command.WishlistItemId));
        }

        if (wishlist.UserId != command.UserId)
        {
            return Result.Failure<WishlistDetailDto>(Error.Validation("Wishlist", "You can only modify your own wishlists"));
        }

        wishlist.RemoveItem(command.WishlistItemId);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch with product details
        var detailSpec = new WishlistDetailByIdSpec(wishlist.Id);
        var detailWishlist = await _wishlistRepository.FirstOrDefaultAsync(detailSpec, ct);

        return Result.Success(WishlistMapper.ToDetailDto(detailWishlist!));
    }
}
