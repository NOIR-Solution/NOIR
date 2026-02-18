namespace NOIR.Application.Features.Wishlists.Commands.MoveToCart;

/// <summary>
/// Handler for moving a wishlist item to the cart.
/// Adds the item to the user's active cart and removes it from the wishlist.
/// </summary>
public sealed class MoveToCartCommandHandler
{
    private readonly IRepository<Domain.Entities.Wishlist.Wishlist, Guid> _wishlistRepository;
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveToCartCommandHandler> _logger;

    public MoveToCartCommandHandler(
        IRepository<Domain.Entities.Wishlist.Wishlist, Guid> wishlistRepository,
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        ILogger<MoveToCartCommandHandler> logger)
    {
        _wishlistRepository = wishlistRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<WishlistDetailDto>> Handle(MoveToCartCommand command, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command.UserId))
        {
            return Result.Failure<WishlistDetailDto>(Error.Validation("UserId", "User must be authenticated"));
        }

        // Find the wishlist containing this item
        var wishlistSpec = new WishlistItemByIdSpec(command.WishlistItemId);
        var wishlist = await _wishlistRepository.FirstOrDefaultAsync(wishlistSpec, ct);

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

        // Get product details for the cart
        var productSpec = new ProductWithVariantByIdSpec(item.ProductId, item.ProductVariantId ?? Guid.Empty);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, ct);

        if (product is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.NotFound("Product", item.ProductId));
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId)
                      ?? product.Variants.FirstOrDefault();

        if (variant is null)
        {
            return Result.Failure<WishlistDetailDto>(Error.Validation("Product", "Product has no available variants"));
        }

        // Get or create cart
        var cartSpec = new ActiveCartByUserIdSpec(command.UserId, forUpdate: true);
        var cart = await _cartRepository.FirstOrDefaultAsync(cartSpec, ct);

        if (cart is null)
        {
            cart = Domain.Entities.Cart.Cart.CreateForUser(command.UserId);
            await _cartRepository.AddAsync(cart, ct);
        }

        // Add to cart
        cart.AddItem(
            productId: product.Id,
            productVariantId: variant.Id,
            productName: product.Name,
            variantName: variant.Name,
            unitPrice: variant.Price,
            quantity: 1,
            imageUrl: product.PrimaryImage?.Url);

        // Remove from wishlist
        wishlist.RemoveItem(command.WishlistItemId);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Moved wishlist item {ItemId} to cart for user {UserId}",
            command.WishlistItemId, command.UserId);

        // Re-fetch with product details
        var detailSpec = new WishlistDetailByIdSpec(wishlist.Id);
        var detailWishlist = await _wishlistRepository.FirstOrDefaultAsync(detailSpec, ct);

        return Result.Success(WishlistMapper.ToDetailDto(detailWishlist!));
    }
}
