namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlistItemPriority;

/// <summary>
/// Handler for updating a wishlist item's priority.
/// </summary>
public sealed class UpdateWishlistItemPriorityCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWishlistItemPriorityCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDetailDto>> Handle(UpdateWishlistItemPriorityCommand command, CancellationToken ct)
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

        var item = wishlist.Items.FirstOrDefault(i => i.Id == command.WishlistItemId);
        if (item is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("WishlistItem", command.WishlistItemId));
        }

        item.UpdatePriority(command.Priority);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch with product details
        var detailSpec = new WishlistDetailByIdSpec(wishlist.Id);
        var detailWishlist = await _wishlistRepository.FirstOrDefaultAsync(detailSpec, ct);

        return Result.Success(WishlistMapper.ToDetailDto(detailWishlist!));
    }
}
