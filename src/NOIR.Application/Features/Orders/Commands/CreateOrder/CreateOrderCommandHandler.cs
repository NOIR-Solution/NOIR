namespace NOIR.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Wolverine handler for creating a new order.
/// </summary>
public class CreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Validate items
        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Items", "Order must contain at least one item.", "NOIR-ORDER-001"));
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(tenantId, cancellationToken);

        // Calculate subtotal
        var subTotal = command.Items.Sum(i => i.UnitPrice * i.Quantity);

        // Calculate grand total
        var grandTotal = subTotal - command.DiscountAmount + command.ShippingAmount;

        // Create the order
        var order = Order.Create(
            orderNumber,
            command.CustomerEmail,
            subTotal,
            grandTotal,
            command.Currency,
            tenantId);

        // Set customer info
        Guid? customerId = _currentUser.IsAuthenticated ? Guid.TryParse(_currentUser.UserId, out var uid) ? uid : null : null;
        order.SetCustomerInfo(customerId, command.CustomerName, command.CustomerPhone);

        // Set shipping address
        var shippingAddress = OrderMapper.ToAddress(command.ShippingAddress);
        order.SetShippingAddress(shippingAddress);

        // Set billing address
        if (command.BillingAddress is not null)
        {
            var billingAddress = OrderMapper.ToAddress(command.BillingAddress);
            order.SetBillingAddress(billingAddress);
        }
        else
        {
            // Use shipping address as billing if not provided
            order.SetBillingAddress(shippingAddress);
        }

        // Set shipping details
        if (!string.IsNullOrEmpty(command.ShippingMethod))
        {
            order.SetShippingDetails(command.ShippingMethod, command.ShippingAmount);
        }

        // Set discount
        if (command.DiscountAmount > 0 || !string.IsNullOrEmpty(command.CouponCode))
        {
            order.SetDiscount(command.DiscountAmount, command.CouponCode);
        }

        // Set customer notes
        if (!string.IsNullOrEmpty(command.CustomerNotes))
        {
            order.SetCustomerNotes(command.CustomerNotes);
        }

        // Set checkout session reference
        if (command.CheckoutSessionId.HasValue)
        {
            order.SetCheckoutSessionId(command.CheckoutSessionId.Value);
        }

        // Add items
        foreach (var itemDto in command.Items)
        {
            order.AddItem(
                itemDto.ProductId,
                itemDto.ProductVariantId,
                itemDto.ProductName,
                itemDto.VariantName,
                itemDto.UnitPrice,
                itemDto.Quantity,
                itemDto.Sku,
                itemDto.ImageUrl,
                itemDto.OptionsSnapshot);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
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
