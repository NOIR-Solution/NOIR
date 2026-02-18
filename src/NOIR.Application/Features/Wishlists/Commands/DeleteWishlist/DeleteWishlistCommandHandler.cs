namespace NOIR.Application.Features.Wishlists.Commands.DeleteWishlist;

/// <summary>
/// Handler for soft deleting a wishlist.
/// </summary>
public sealed class DeleteWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(DeleteWishlistCommand command, CancellationToken ct)
    {
        var spec = new WishlistByIdSpec(command.Id, forUpdate: true);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDto>(Error.NotFound("Wishlist", command.Id));
        }

        if (wishlist.UserId != command.UserId)
        {
            return Result.Failure<WishlistDto>(Error.Validation("Wishlist", "You can only delete your own wishlists"));
        }

        if (wishlist.IsDefault)
        {
            return Result.Failure<WishlistDto>(Error.Validation("Wishlist", "Cannot delete the default wishlist"));
        }

        var dto = WishlistMapper.ToDto(wishlist);
        _wishlistRepository.Remove(wishlist);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(dto);
    }
}
