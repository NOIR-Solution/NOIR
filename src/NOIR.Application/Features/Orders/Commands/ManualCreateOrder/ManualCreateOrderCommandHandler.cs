namespace NOIR.Application.Features.Orders.Commands.ManualCreateOrder;

/// <summary>
/// Wolverine handler for manually creating a new order.
/// Resolves product data from variants, creates order with snapshots.
/// </summary>
public class ManualCreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<PaymentGateway, Guid> _gatewayRepository;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryMovementLogger _movementLogger;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ManualCreateOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<PaymentGateway, Guid> gatewayRepository,
        IPaymentService paymentService,
        IInventoryMovementLogger movementLogger,
        IOrderNumberGenerator orderNumberGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _paymentRepository = paymentRepository;
        _gatewayRepository = gatewayRepository;
        _paymentService = paymentService;
        _movementLogger = movementLogger;
        _orderNumberGenerator = orderNumberGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(
        ManualCreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Validate items
        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Items", "Order must contain at least one item.", ErrorCodes.Order.MustHaveItems));
        }

        // Load products that contain the requested variants
        var variantIds = command.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var productSpec = new ProductsByVariantIdsForUpdateSpec(variantIds);
        var products = await _productRepository.ListAsync(productSpec, cancellationToken);

        // Build variant lookup from loaded products
        var variantLookup = products
            .SelectMany(p => p.Variants.Where(v => variantIds.Contains(v.Id)))
            .ToDictionary(v => v.Id);

        // Validate all variants exist
        var missingVariantIds = variantIds.Where(id => !variantLookup.ContainsKey(id)).ToList();
        if (missingVariantIds.Count > 0)
        {
            return Result.Failure<OrderDto>(
                Error.NotFound("ProductVariant", $"Product variants not found: {string.Join(", ", missingVariantIds)}", ErrorCodes.Product.VariantNotFound));
        }

        // Validate all variants belong to active products
        var inactiveProducts = products.Where(p => p.Status != ProductStatus.Active).ToList();
        if (inactiveProducts.Count > 0)
        {
            var inactiveNames = inactiveProducts.Select(p => p.Name).Distinct();
            return Result.Failure<OrderDto>(
                Error.Validation("Items", $"Products are not active: {string.Join(", ", inactiveNames)}", ErrorCodes.Product.InvalidStatus));
        }

        // Generate order number atomically via database sequence
        var orderNumber = await _orderNumberGenerator.GenerateNextAsync(tenantId, cancellationToken);

        // Resolve items and calculate initial subtotal
        // Note: Order.AddItem calls RecalculateSubTotal which will recalculate these
        var subTotal = 0m;
        var resolvedItems = new List<(ManualOrderItemDto Item, ProductVariant Variant, Product Product, decimal ResolvedPrice)>();
        foreach (var item in command.Items)
        {
            var variant = variantLookup[item.ProductVariantId];
            var product = products.First(p => p.Variants.Any(v => v.Id == variant.Id));
            var resolvedPrice = item.UnitPrice ?? variant.Price;
            subTotal += resolvedPrice * item.Quantity;
            resolvedItems.Add((item, variant, product, resolvedPrice));
        }

        // Validate stock availability
        foreach (var (item, variant, product, resolvedPrice) in resolvedItems)
        {
            if (variant.StockQuantity < item.Quantity)
            {
                return Result.Failure<OrderDto>(
                    Error.Validation("Items",
                        $"Insufficient stock for '{product.Name} - {variant.Name}'. Available: {variant.StockQuantity}, Requested: {item.Quantity}",
                        ErrorCodes.Order.InsufficientStock));
            }
        }

        // Calculate grand total (will be recalculated by Order entity methods)
        var grandTotal = subTotal - command.DiscountAmount + command.ShippingAmount + command.TaxAmount;

        // Create the order
        var order = Order.Create(
            orderNumber,
            command.CustomerEmail,
            subTotal,
            grandTotal,
            command.Currency,
            tenantId);

        // Set customer info
        order.SetCustomerInfo(command.CustomerId, command.CustomerName, command.CustomerPhone);

        // Set shipping address
        if (command.ShippingAddress is not null)
        {
            var shippingAddress = OrderMapper.ToAddress(command.ShippingAddress);
            order.SetShippingAddress(shippingAddress);

            // Use shipping address as billing if not provided
            if (command.BillingAddress is null)
            {
                order.SetBillingAddress(shippingAddress);
            }
        }

        // Set billing address
        if (command.BillingAddress is not null)
        {
            var billingAddress = OrderMapper.ToAddress(command.BillingAddress);
            order.SetBillingAddress(billingAddress);
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

        // Set tax
        if (command.TaxAmount > 0)
        {
            order.SetTax(command.TaxAmount);
        }

        // Set customer notes
        if (!string.IsNullOrEmpty(command.CustomerNotes))
        {
            order.SetCustomerNotes(command.CustomerNotes);
        }

        // Add items with product data snapshots
        foreach (var (item, variant, product, resolvedPrice) in resolvedItems)
        {
            var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary) ?? product.Images.FirstOrDefault();

            var orderItem = order.AddItem(
                product.Id,
                variant.Id,
                product.Name,
                variant.Name,
                resolvedPrice,
                item.Quantity,
                variant.Sku,
                primaryImage?.Url,
                variant.OptionsJson);

            // Apply item-level discount if specified
            if (item.DiscountAmount > 0)
            {
                orderItem.SetDiscount(item.DiscountAmount);
            }
        }

        // Reserve inventory for each item
        foreach (var (item, variant, product, resolvedPrice) in resolvedItems)
        {
            var quantityBefore = variant.StockQuantity;
            variant.ReserveStock(item.Quantity);

            await _movementLogger.LogMovementAsync(
                variant,
                InventoryMovementType.Reservation,
                quantityBefore,
                item.Quantity,
                reference: orderNumber,
                notes: $"Reserved for manual order {orderNumber}",
                userId: command.UserId,
                cancellationToken: cancellationToken);
        }

        // Add internal notes
        if (!string.IsNullOrEmpty(command.InternalNotes))
        {
            order.AddInternalNote(command.InternalNotes);
        }

        // Add automatic internal note about manual creation
        var creatorEmail = _currentUser.Email ?? "unknown";
        order.AddInternalNote($"Order manually created by {creatorEmail}");

        // If initial payment status is Paid, confirm the order
        if (command.InitialPaymentStatus == PaymentStatus.Paid)
        {
            order.Confirm();
        }

        // Create PaymentTransaction if payment method is specified
        if (command.PaymentMethod.HasValue)
        {
            var gatewaySpec = new PaymentGatewayByProviderSpec("manual");
            var gateway = await _gatewayRepository.FirstOrDefaultAsync(gatewaySpec, cancellationToken);

            if (gateway == null)
            {
                var codGatewaySpec = new PaymentGatewayByProviderSpec("cod");
                gateway = await _gatewayRepository.FirstOrDefaultAsync(codGatewaySpec, cancellationToken);
            }

            var gatewayId = gateway?.Id ?? Guid.Empty;
            var providerName = gateway?.Provider ?? "manual";

            var transactionNumber = _paymentService.GenerateTransactionNumber();

            var payment = PaymentTransaction.Create(
                transactionNumber,
                gatewayId,
                providerName,
                order.GrandTotal,
                command.Currency ?? "VND",
                command.PaymentMethod.Value,
                Guid.NewGuid().ToString("N"),
                tenantId);

            payment.SetOrderId(order.Id);

            if (command.CustomerId.HasValue)
            {
                payment.SetCustomerId(command.CustomerId.Value);
            }

            if (command.InitialPaymentStatus == PaymentStatus.Paid)
            {
                payment.MarkAsPaid("MANUAL");
            }
            else if (command.PaymentMethod.Value == PaymentMethod.COD)
            {
                payment.MarkAsCodPending();
            }

            await _paymentRepository.AddAsync(payment, cancellationToken);
        }

        // Save order - no retry loop needed, order number is atomically generated
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
