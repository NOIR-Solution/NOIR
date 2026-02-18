namespace NOIR.Domain.Entities.Order;

/// <summary>
/// Represents a customer order.
/// Tracks order lifecycle from creation to completion.
/// </summary>
public class Order : TenantAggregateRoot<Guid>
{
    private Order() : base() { }
    private Order(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Unique order number for display (e.g., "ORD-20250126-0001").
    /// </summary>
    public string OrderNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Customer ID if registered user, null for guest orders.
    /// </summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>
    /// Current order status.
    /// </summary>
    public OrderStatus Status { get; private set; }

    // Financial
    /// <summary>
    /// Sum of all line items before discounts/shipping/tax.
    /// </summary>
    public decimal SubTotal { get; private set; }

    /// <summary>
    /// Total discount applied to the order.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>
    /// Total tax amount.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Final total: SubTotal - DiscountAmount + ShippingAmount + TaxAmount.
    /// </summary>
    public decimal GrandTotal { get; private set; }

    /// <summary>
    /// Currency code (default: VND).
    /// </summary>
    public string Currency { get; private set; } = "VND";

    // Addresses (Owned types)
    /// <summary>
    /// Shipping address.
    /// </summary>
    public Address? ShippingAddress { get; private set; }

    /// <summary>
    /// Billing address (can be same as shipping).
    /// </summary>
    public Address? BillingAddress { get; private set; }

    // Shipping
    /// <summary>
    /// Selected shipping method name.
    /// </summary>
    public string? ShippingMethod { get; private set; }

    /// <summary>
    /// Tracking number from carrier.
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// Shipping carrier name.
    /// </summary>
    public string? ShippingCarrier { get; private set; }

    /// <summary>
    /// Estimated delivery date.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryAt { get; private set; }

    // Customer Info (denormalized for guest orders)
    /// <summary>
    /// Customer email address.
    /// </summary>
    public string CustomerEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string? CustomerPhone { get; private set; }

    /// <summary>
    /// Customer display name.
    /// </summary>
    public string? CustomerName { get; private set; }

    // Notes
    /// <summary>
    /// Customer notes/instructions.
    /// </summary>
    public string? CustomerNotes { get; private set; }

    /// <summary>
    /// Internal staff notes.
    /// </summary>
    public string? InternalNotes { get; private set; }

    // Cancellation
    /// <summary>
    /// Reason for cancellation if applicable.
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// When the order was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    // Return
    /// <summary>
    /// Reason for return if applicable.
    /// </summary>
    public string? ReturnReason { get; private set; }

    /// <summary>
    /// When the order was returned.
    /// </summary>
    public DateTimeOffset? ReturnedAt { get; private set; }

    // Timestamps
    /// <summary>
    /// When the order was confirmed (payment received).
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// When the order was shipped.
    /// </summary>
    public DateTimeOffset? ShippedAt { get; private set; }

    /// <summary>
    /// When the order was delivered.
    /// </summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>
    /// When the order was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    // Coupon
    /// <summary>
    /// Applied coupon code.
    /// </summary>
    public string? CouponCode { get; private set; }

    /// <summary>
    /// Reference to the checkout session that created this order.
    /// </summary>
    public Guid? CheckoutSessionId { get; private set; }

    // Navigation
    public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public virtual ICollection<Payment.PaymentTransaction> Payments { get; private set; } = new List<Payment.PaymentTransaction>();

    /// <summary>
    /// Creates a new order.
    /// </summary>
    public static Order Create(
        string orderNumber,
        string customerEmail,
        decimal subTotal,
        decimal grandTotal,
        string currency = "VND",
        string? tenantId = null)
    {
        var order = new Order(Guid.NewGuid(), tenantId)
        {
            OrderNumber = orderNumber,
            CustomerEmail = customerEmail,
            SubTotal = subTotal,
            GrandTotal = grandTotal,
            Currency = currency,
            Status = OrderStatus.Pending,
            DiscountAmount = 0,
            ShippingAmount = 0,
            TaxAmount = 0
        };

        order.AddDomainEvent(new OrderCreatedEvent(
            order.Id, orderNumber, customerEmail, grandTotal, currency));
        return order;
    }

    /// <summary>
    /// Sets customer information.
    /// </summary>
    public void SetCustomerInfo(Guid? customerId, string? customerName, string? customerPhone)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
    }

    /// <summary>
    /// Sets the shipping address.
    /// </summary>
    public void SetShippingAddress(Address address)
    {
        ShippingAddress = address;
    }

    /// <summary>
    /// Sets the billing address.
    /// </summary>
    public void SetBillingAddress(Address address)
    {
        BillingAddress = address;
    }

    /// <summary>
    /// Sets shipping details.
    /// </summary>
    public void SetShippingDetails(
        string shippingMethod,
        decimal shippingAmount,
        DateTimeOffset? estimatedDeliveryAt = null)
    {
        ShippingMethod = shippingMethod;
        ShippingAmount = shippingAmount;
        EstimatedDeliveryAt = estimatedDeliveryAt;
        RecalculateGrandTotal();
    }

    /// <summary>
    /// Sets discount details.
    /// </summary>
    public void SetDiscount(decimal discountAmount, string? couponCode = null)
    {
        DiscountAmount = discountAmount;
        CouponCode = couponCode;
        RecalculateGrandTotal();
    }

    /// <summary>
    /// Sets tax amount.
    /// </summary>
    public void SetTax(decimal taxAmount)
    {
        TaxAmount = taxAmount;
        RecalculateGrandTotal();
    }

    /// <summary>
    /// Sets customer notes.
    /// </summary>
    public void SetCustomerNotes(string? notes)
    {
        CustomerNotes = notes;
    }

    /// <summary>
    /// Sets the checkout session reference.
    /// </summary>
    public void SetCheckoutSessionId(Guid checkoutSessionId)
    {
        CheckoutSessionId = checkoutSessionId;
    }

    /// <summary>
    /// Adds an item to the order.
    /// </summary>
    public OrderItem AddItem(
        Guid productId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity,
        string? sku = null,
        string? imageUrl = null,
        string? optionsSnapshot = null)
    {
        var item = OrderItem.Create(
            Id,
            productId,
            productVariantId,
            productName,
            variantName,
            unitPrice,
            quantity,
            sku,
            imageUrl,
            optionsSnapshot,
            TenantId);

        Items.Add(item);
        RecalculateSubTotal();
        return item;
    }

    /// <summary>
    /// Confirms the order (payment received).
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber));
    }

    /// <summary>
    /// Starts processing the order.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot start processing order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Processing;
        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
    }

    /// <summary>
    /// Ships the order.
    /// </summary>
    public void Ship(string trackingNumber, string shippingCarrier)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException($"Cannot ship order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Shipped;
        ShippedAt = DateTimeOffset.UtcNow;
        TrackingNumber = trackingNumber;
        ShippingCarrier = shippingCarrier;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
        AddDomainEvent(new OrderShippedEvent(Id, OrderNumber, trackingNumber, shippingCarrier));
    }

    /// <summary>
    /// Marks the order as delivered.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot mark as delivered order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
        AddDomainEvent(new OrderDeliveredEvent(Id, OrderNumber));
    }

    /// <summary>
    /// Completes the order.
    /// </summary>
    public void Complete()
    {
        if (Status != OrderStatus.Delivered)
            throw new InvalidOperationException($"Cannot complete order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
        AddDomainEvent(new OrderCompletedEvent(Id, OrderNumber));
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status, reason));
        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, reason));
    }

    /// <summary>
    /// Returns the order.
    /// </summary>
    public void Return(string? reason = null)
    {
        if (Status is not (OrderStatus.Delivered or OrderStatus.Completed))
            throw new InvalidOperationException($"Cannot return order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Returned;
        ReturnReason = reason;
        ReturnedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status, reason));
        AddDomainEvent(new OrderReturnedEvent(Id, OrderNumber, reason));
    }

    /// <summary>
    /// Marks the order as refunded.
    /// </summary>
    public void MarkAsRefunded(decimal refundAmount)
    {
        if (Status is OrderStatus.Pending or OrderStatus.Cancelled or OrderStatus.Refunded)
            throw new InvalidOperationException($"Cannot refund order in {Status} status");

        var oldStatus = Status;
        Status = OrderStatus.Refunded;

        AddDomainEvent(new OrderStatusChangedEvent(Id, OrderNumber, oldStatus, Status));
        AddDomainEvent(new OrderRefundedEvent(Id, OrderNumber, refundAmount));
    }

    /// <summary>
    /// Adds internal notes.
    /// </summary>
    public void AddInternalNote(string note)
    {
        InternalNotes = string.IsNullOrEmpty(InternalNotes)
            ? note
            : $"{InternalNotes}\n---\n{note}";

        AddDomainEvent(new OrderNoteAddedEvent(Id, note, true));
    }

    /// <summary>
    /// Recalculates subtotal from items.
    /// </summary>
    private void RecalculateSubTotal()
    {
        SubTotal = Items.Sum(i => i.Subtotal);
        RecalculateGrandTotal();
    }

    /// <summary>
    /// Recalculates grand total.
    /// </summary>
    private void RecalculateGrandTotal()
    {
        GrandTotal = SubTotal - DiscountAmount + ShippingAmount + TaxAmount;
    }
}
