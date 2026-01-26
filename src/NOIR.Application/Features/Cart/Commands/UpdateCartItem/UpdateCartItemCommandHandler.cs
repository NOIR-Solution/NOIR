namespace NOIR.Application.Features.Cart.Commands.UpdateCartItem;

/// <summary>
/// Handler for updating cart item quantity.
/// </summary>
public sealed class UpdateCartItemCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCartItemCommandHandler> _logger;

    public UpdateCartItemCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCartItemCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartDto>> Handle(UpdateCartItemCommand command, CancellationToken ct)
    {
        // 1. Get the cart
        var cartSpec = new CartByIdSpec(command.CartId, forUpdate: true);
        var cart = await _cartRepository.FirstOrDefaultAsync(cartSpec, ct);

        if (cart is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("Cart", "Cart not found"));
        }

        // 2. Verify ownership
        if (!cart.IsOwnedBy(command.UserId, command.SessionId))
        {
            return Result.Failure<CartDto>(Error.Forbidden("Cart", "You do not have access to this cart"));
        }

        // 3. Update item quantity (or remove if quantity is 0)
        try
        {
            if (command.Quantity == 0)
            {
                cart.RemoveItem(command.ItemId);
                _logger.LogInformation("Removed item {ItemId} from cart {CartId}", command.ItemId, cart.Id);
            }
            else
            {
                cart.UpdateItemQuantity(command.ItemId, command.Quantity);
                _logger.LogInformation("Updated item {ItemId} quantity to {Quantity} in cart {CartId}",
                    command.ItemId, command.Quantity, cart.Id);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(Error.Validation("CartItem", ex.Message));
        }

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(CartMapper.ToDto(cart));
    }
}
