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
    private readonly ITenantInfo _tenantInfo;

    public CompleteCheckoutCommandHandler(
        IRepository<CheckoutSession, Guid> checkoutRepository,
        IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository,
        IRepository<Domain.Entities.Order.Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        ITenantInfo tenantInfo)
    {
        _checkoutRepository = checkoutRepository;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _tenantInfo = tenantInfo;
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

            // Generate order number
            var orderNumber = GenerateOrderNumber();

            // Create order
            var order = Domain.Entities.Order.Order.Create(
                orderNumber: orderNumber,
                customerEmail: session.CustomerEmail,
                subTotal: session.SubTotal,
                grandTotal: session.GrandTotal,
                currency: session.Currency,
                tenantId: _tenantInfo.Id);

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
                order.AddItem(
                    productId: cartItem.ProductId,
                    productVariantId: cartItem.ProductVariantId,
                    productName: cartItem.ProductName,
                    variantName: cartItem.VariantName,
                    unitPrice: cartItem.UnitPrice,
                    quantity: cartItem.Quantity,
                    sku: null, // Would need to look up from product
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

    private static string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-XXXX (where XXXX is random hex)
        var datePart = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"ORD-{datePart}-{randomPart}";
    }
}
