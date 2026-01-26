namespace NOIR.Application.Features.Cart.Commands.ClearCart;

/// <summary>
/// Handler for clearing all items from a cart.
/// </summary>
public sealed class ClearCartCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClearCartCommandHandler> _logger;

    public ClearCartCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClearCartCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartDto>> Handle(ClearCartCommand command, CancellationToken ct)
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

        // 3. Clear cart
        try
        {
            cart.Clear();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CartDto>(Error.Validation("Cart", ex.Message));
        }

        // 4. Save changes
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Cleared cart {CartId}", cart.Id);

        return Result.Success(CartMapper.ToDto(cart));
    }
}
