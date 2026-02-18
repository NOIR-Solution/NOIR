namespace NOIR.Application.Features.Wishlists.Commands.ShareWishlist;

/// <summary>
/// Handler for generating a share token for a wishlist.
/// </summary>
public sealed class ShareWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShareWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(ShareWishlistCommand command, CancellationToken ct)
    {
        var spec = new WishlistByIdSpec(command.WishlistId, forUpdate: true);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDto>(Error.NotFound("Wishlist", command.WishlistId));
        }

        if (wishlist.UserId != command.UserId)
        {
            return Result.Failure<WishlistDto>(Error.Validation("Wishlist", "You can only share your own wishlists"));
        }

        wishlist.GenerateShareToken();
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(WishlistMapper.ToDto(wishlist));
    }
}
