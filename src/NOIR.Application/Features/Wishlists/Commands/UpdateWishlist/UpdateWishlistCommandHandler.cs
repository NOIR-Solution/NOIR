namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlist;

/// <summary>
/// Handler for updating a wishlist.
/// </summary>
public sealed class UpdateWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(UpdateWishlistCommand command, CancellationToken ct)
    {
        var spec = new WishlistByIdSpec(command.Id, forUpdate: true);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

        if (wishlist is null)
        {
            return Result.Failure<WishlistDto>(Error.NotFound("Wishlist", command.Id));
        }

        if (wishlist.UserId != command.UserId)
        {
            return Result.Failure<WishlistDto>(Error.Validation("Wishlist", "You can only update your own wishlists"));
        }

        wishlist.UpdateName(command.Name);
        wishlist.SetPublic(command.IsPublic);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(WishlistMapper.ToDto(wishlist));
    }
}
