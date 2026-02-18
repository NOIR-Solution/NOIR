namespace NOIR.Application.Features.Wishlists.Commands.CreateWishlist;

/// <summary>
/// Handler for creating a new wishlist.
/// </summary>
public sealed class CreateWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDto>> Handle(CreateWishlistCommand command, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<WishlistDto>(Error.Validation("UserId", "User must be authenticated"));
        }

        // Check if user already has a default wishlist
        var defaultSpec = new DefaultWishlistByUserSpec(command.UserId);
        var hasDefault = await _wishlistRepository.AnyAsync(defaultSpec, ct);

        var wishlist = Domain.Entities.Wishlist.Wishlist.Create(
            command.UserId,
            command.Name,
            isDefault: !hasDefault);

        if (command.IsPublic)
        {
            wishlist.SetPublic(true);
        }

        await _wishlistRepository.AddAsync(wishlist, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(WishlistMapper.ToDto(wishlist));
    }
}
