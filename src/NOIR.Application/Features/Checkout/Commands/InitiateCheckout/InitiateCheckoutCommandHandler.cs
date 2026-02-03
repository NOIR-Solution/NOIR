namespace NOIR.Application.Features.Checkout.Commands.InitiateCheckout;

/// <summary>
/// Wolverine handler for initiating a checkout session.
/// </summary>
public class InitiateCheckoutCommandHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public InitiateCheckoutCommandHandler(
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _cartRepository = cartRepository;
        _checkoutRepository = checkoutRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        InitiateCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        // Get cart with items
        var cartSpec = new CartByIdWithItemsSpec(command.CartId);
        var cart = await _cartRepository.FirstOrDefaultAsync(cartSpec, cancellationToken);

        if (cart is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Cart with ID '{command.CartId}' not found.", "NOIR-CHECKOUT-001"));
        }

        if (cart.IsEmpty)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("CartId", "Cannot checkout an empty cart.", "NOIR-CHECKOUT-002"));
        }

        if (cart.Status != CartStatus.Active)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("CartId", $"Cannot checkout a cart in {cart.Status} status.", "NOIR-CHECKOUT-003"));
        }

        // Check for existing active checkout session
        var existingSessionSpec = new ActiveCheckoutSessionByCartIdSpec(command.CartId);
        var existingSession = await _checkoutRepository.FirstOrDefaultAsync(existingSessionSpec, cancellationToken);

        if (existingSession is not null && !existingSession.IsExpired)
        {
            // Return existing session instead of creating new one
            return Result.Success(CheckoutMapper.ToDto(existingSession));
        }

        // If existing session is expired, mark it
        if (existingSession is not null)
        {
            existingSession.MarkAsExpired();
        }

        // Create new checkout session
        var session = CheckoutSession.Create(
            cartId: command.CartId,
            customerEmail: command.CustomerEmail,
            subTotal: cart.Subtotal,
            currency: cart.Currency,
            userId: command.UserId,
            tenantId: _currentUser.TenantId);

        // Set customer info if provided
        if (!string.IsNullOrEmpty(command.CustomerName) || !string.IsNullOrEmpty(command.CustomerPhone))
        {
            session.SetCustomerInfo(
                command.CustomerName ?? string.Empty,
                command.CustomerPhone ?? string.Empty);
        }

        await _checkoutRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CheckoutMapper.ToDto(session));
    }
}
