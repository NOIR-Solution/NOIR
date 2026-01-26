using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.Features.Cart.Commands.AddToCart;

/// <summary>
/// Handler for adding items to cart.
/// </summary>
public sealed class AddToCartCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddToCartCommandHandler> _logger;

    public AddToCartCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddToCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartDto>> Handle(AddToCartCommand command, CancellationToken ct)
    {
        // 1. Get the product and variant
        var productSpec = new ProductWithVariantByIdSpec(command.ProductId, command.ProductVariantId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, ct);

        if (product is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("Product", command.ProductId));
        }

        var variant = product.Variants.FirstOrDefault(v => v.Id == command.ProductVariantId);
        if (variant is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("ProductVariant", command.ProductVariantId));
        }

        // 2. Check product is active
        if (product.Status != ProductStatus.Active)
        {
            return Result.Failure<CartDto>(Error.Validation("Product", "Product is not available for purchase"));
        }

        // 3. Check stock availability
        if (product.TrackInventory && variant.StockQuantity < command.Quantity)
        {
            return Result.Failure<CartDto>(Error.Validation("Quantity",
                $"Insufficient stock. Available: {variant.StockQuantity}"));
        }

        // 4. Get or create cart
        Domain.Entities.Cart.Cart? cart = null;

        if (!string.IsNullOrEmpty(command.UserId))
        {
            var userCartSpec = new ActiveCartByUserIdSpec(command.UserId, forUpdate: true);
            cart = await _cartRepository.FirstOrDefaultAsync(userCartSpec, ct);

            if (cart is null)
            {
                cart = Domain.Entities.Cart.Cart.CreateForUser(command.UserId);
                await _cartRepository.AddAsync(cart, ct);
            }
        }
        else if (!string.IsNullOrEmpty(command.SessionId))
        {
            var sessionCartSpec = new ActiveCartBySessionIdSpec(command.SessionId, forUpdate: true);
            cart = await _cartRepository.FirstOrDefaultAsync(sessionCartSpec, ct);

            if (cart is null)
            {
                cart = Domain.Entities.Cart.Cart.CreateForGuest(command.SessionId);
                await _cartRepository.AddAsync(cart, ct);
            }
        }

        if (cart is null)
        {
            return Result.Failure<CartDto>(Error.Validation("Cart", "Unable to create or find cart"));
        }

        // 5. Add item to cart
        var imageUrl = product.PrimaryImage?.Url;

        cart.AddItem(
            productId: product.Id,
            productVariantId: variant.Id,
            productName: product.Name,
            variantName: variant.Name,
            unitPrice: variant.Price,
            quantity: command.Quantity,
            imageUrl: imageUrl);

        // 6. Save changes
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Added {Quantity} of product {ProductId} variant {VariantId} to cart {CartId}",
            command.Quantity, product.Id, variant.Id, cart.Id);

        return Result.Success(CartMapper.ToDto(cart));
    }
}
