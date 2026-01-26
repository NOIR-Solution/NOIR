namespace NOIR.Application.Features.Cart.Commands.RemoveCartItem;

/// <summary>
/// Handler for removing items from cart.
/// </summary>
public sealed class RemoveCartItemCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveCartItemCommandHandler> _logger;

    public RemoveCartItemCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveCartItemCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand command, CancellationToken ct)
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

        // 3. Remove item
        try
        {
            cart.RemoveItem(command.ItemId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(Error.Validation("CartItem", ex.Message));
        }

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Removed item {ItemId} from cart {CartId}", command.ItemId, cart.Id);

        return Result.Success(CartMapper.ToDto(cart));
    }
}
