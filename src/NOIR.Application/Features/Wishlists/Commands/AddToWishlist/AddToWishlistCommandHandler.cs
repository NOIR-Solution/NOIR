namespace NOIR.Application.Features.Wishlists.Commands.AddToWishlist;

/// <summary>
/// Handler for adding a product to a wishlist.
/// </summary>
public sealed class AddToWishlistCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddToWishlistCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WishlistDetailDto>> Handle(AddToWishlistCommand command, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<WishlistDetailDto>(Error.Validation("UserId", "User must be authenticated"));
        }

        // Verify product exists
        var productExists = await _productRepository.ExistsAsync(command.ProductId, ct);
        if (!productExists)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("Product", command.ProductId));
        }

        // Get or create target wishlist
        Domain.Entities.Wishlist.Wishlist? wishlist;

        if (command.WishlistId.HasValue)
        {
            var spec = new WishlistByIdSpec(command.WishlistId.Value, forUpdate: true);
            wishlist = await _wishlistRepository.FirstOrDefaultAsync(spec, ct);

            if (wishlist is null)
            {
                return Result.Failure<WishlistDetailDto>(Error.NotFound("Wishlist", command.WishlistId.Value));
            }

            if (wishlist.UserId != command.UserId)
            {
                return Result.Failure<WishlistDetailDto>(Error.Validation("Wishlist", "You can only add to your own wishlists"));
            }
        }
        else
        {
            // Use default wishlist, create if needed
            var defaultSpec = new DefaultWishlistByUserSpec(command.UserId, forUpdate: true);
            wishlist = await _wishlistRepository.FirstOrDefaultAsync(defaultSpec, ct);

            if (wishlist is null)
            {
                wishlist = Domain.Entities.Wishlist.Wishlist.Create(command.UserId, "My Wishlist", isDefault: true);
                await _wishlistRepository.AddAsync(wishlist, ct);
            }
        }

        // Add item
        wishlist.AddItem(command.ProductId, command.ProductVariantId, command.Note);
        await _unitOfWork.SaveChangesAsync(ct);

        // Re-fetch with product details for the response
        var detailSpec = new WishlistDetailByIdSpec(wishlist.Id);
        var detailWishlist = await _wishlistRepository.FirstOrDefaultAsync(detailSpec, ct);

        return Result.Success(WishlistMapper.ToDetailDto(detailWishlist!));
    }
}
