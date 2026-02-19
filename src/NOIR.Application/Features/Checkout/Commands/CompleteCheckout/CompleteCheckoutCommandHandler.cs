namespace NOIR.Application.Features.Checkout.Commands.CompleteCheckout;

/// <summary>
/// Wolverine handler for completing a checkout session.
/// Creates the order from checkout session data.
/// </summary>
public class CompleteCheckoutCommandHandler
{
    private readonly IRepository<CheckoutSession, Guid> _checkoutRepository;
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;
    private readonly IRepository<Domain.Entities.Order.Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CompleteCheckoutCommandHandler(
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IRepository<Domain.Entities.Order.Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _checkoutRepository = checkoutRepository;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CheckoutSessionDto>> Handle(
        CompleteCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        // Get checkout session with tracking
        var sessionSpec = new CheckoutSessionByIdForUpdateSpec(command.SessionId);
        var session = await _checkoutRepository.FirstOrDefaultAsync(sessionSpec, cancellationToken);

        if (session is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Checkout session with ID '{command.SessionId}' not found.", "NOIR-CHECKOUT-014"));
        }

        if (session.IsExpired)
        {
            session.MarkAsExpired();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", "Checkout session has expired.", "NOIR-CHECKOUT-015"));
        }

        // Validate checkout is ready
        if (session.ShippingAddress is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("ShippingAddress", "Shipping address is required to complete checkout.", "NOIR-CHECKOUT-016"));
        }

        if (string.IsNullOrEmpty(session.ShippingMethod))
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("ShippingMethod", "Shipping method must be selected.", "NOIR-CHECKOUT-017"));
        }

        // Get cart with items
        var cartSpec = new CartByIdWithItemsForUpdateSpec(session.CartId);
        var cart = await _cartRepository.FirstOrDefaultAsync(cartSpec, cancellationToken);

        if (cart is null)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.NotFound($"Cart with ID '{session.CartId}' not found.", "NOIR-CHECKOUT-018"));
        }

        if (cart.IsEmpty)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("CartId", "Cannot complete checkout for an empty cart.", "NOIR-CHECKOUT-019"));
        }

        try
        {
            // Set customer notes if provided
            if (!string.IsNullOrEmpty(command.CustomerNotes))
            {
                session.SetCustomerNotes(command.CustomerNotes);
            }

            // Mark session as processing
            if (session.Status is CheckoutSessionStatus.PaymentPending or CheckoutSessionStatus.ShippingSelected)
            {
                session.MarkAsPaymentProcessing();
            }

            // Generate order number using sequence
            var orderNumber = await GenerateOrderNumberAsync(_currentUser.TenantId, cancellationToken);

            // Create order
            var order = Domain.Entities.Order.Order.Create(
                orderNumber: orderNumber,
                customerEmail: session.CustomerEmail,
                subTotal: session.SubTotal,
                grandTotal: session.GrandTotal,
                currency: session.Currency,
                tenantId: _currentUser.TenantId);

            // Set customer info
            if (!string.IsNullOrEmpty(session.UserId) && Guid.TryParse(session.UserId, out var customerId))
            {
                order.SetCustomerInfo(customerId, session.CustomerName, session.CustomerPhone);
            }
            else
            {
                order.SetCustomerInfo(null, session.CustomerName, session.CustomerPhone);
            }

            // Set addresses
            order.SetShippingAddress(session.ShippingAddress);
            if (session.BillingAddress is not null)
            {
                order.SetBillingAddress(session.BillingAddress);
            }

            // Set shipping details
            order.SetShippingDetails(
                session.ShippingMethod,
                session.ShippingCost,
                session.EstimatedDeliveryAt);

            // Set discount if applied
            if (session.DiscountAmount > 0)
            {
                order.SetDiscount(session.DiscountAmount, session.CouponCode);
            }

            // Set tax
            if (session.TaxAmount > 0)
            {
                order.SetTax(session.TaxAmount);
            }

            // Set customer notes
            if (!string.IsNullOrEmpty(session.CustomerNotes))
            {
                order.SetCustomerNotes(session.CustomerNotes);
            }

            // Set checkout session reference
            order.SetCheckoutSessionId(session.Id);

            // Add order items from cart
            foreach (var cartItem in cart.Items)
            {
                // SKU and optionsSnapshot are passed as null because CartItem does not yet store them.
                // To enable SKU-level inventory tracking at checkout, CartItem would need to capture
                // the SKU and selected option values during the AddToCart operation, then forward
                // them here. Until then, inventory is tracked at the ProductVariant level only.
                order.AddItem(
                    productId: cartItem.ProductId,
                    productVariantId: cartItem.ProductVariantId,
                    productName: cartItem.ProductName,
                    variantName: cartItem.VariantName,
                    unitPrice: cartItem.UnitPrice,
                    quantity: cartItem.Quantity,
                    sku: null,
                    imageUrl: cartItem.ImageUrl,
                    optionsSnapshot: null);
            }

            // Save order
            await _orderRepository.AddAsync(order, cancellationToken);

            // Complete checkout session
            session.Complete(order.Id, orderNumber);

            // Mark cart as converted
            cart.MarkAsConverted(order.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(CheckoutMapper.ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CheckoutSessionDto>(
                Error.Validation("SessionId", ex.Message, "NOIR-CHECKOUT-020"));
        }
    }

    private async Task<string> GenerateOrderNumberAsync(string? tenantId, CancellationToken cancellationToken)
    {
        // Format: ORD-YYYYMMDD-XXXX where XXXX is a daily sequence number
        var today = DateTime.UtcNow;
        var datePrefix = $"ORD-{today:yyyyMMdd}-";

        // Get latest order number for today
        var spec = new LatestOrderNumberTodaySpec(datePrefix, tenantId);
        var latestOrder = await _orderRepository.FirstOrDefaultAsync(spec, cancellationToken);

        int sequence = 1;
        if (latestOrder is not null)
        {
            // Extract sequence number from latest order
            var lastSequence = latestOrder.OrderNumber.Split('-').Last();
            if (int.TryParse(lastSequence, out var lastNum))
            {
                sequence = lastNum + 1;
            }
        }

        return $"{datePrefix}{sequence:D4}";
    }
}
