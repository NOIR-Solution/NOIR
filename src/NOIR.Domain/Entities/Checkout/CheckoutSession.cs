namespace NOIR.Domain.Entities.Checkout;

/// <summary>
/// Represents a checkout session.
/// Captures the checkout flow state from cart to order creation.
/// Session expires after 15 minutes of inactivity.
/// </summary>
public class CheckoutSession : TenantAggregateRoot<Guid>
{
    private const int DefaultExpirationMinutes = 15;

    private CheckoutSession() : base() { }
    private CheckoutSession(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Reference to the cart being checked out.
    /// </summary>
    public Guid CartId { get; private set; }

    /// <summary>
    /// User ID if authenticated, null for guest checkout.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Current checkout status.
    /// </summary>
    public CheckoutSessionStatus Status { get; private set; }

    /// <summary>
    /// When the session expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    // Customer Info
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

    // Addresses (Owned types)
    /// <summary>
    /// Shipping address.
    /// </summary>
    public Address? ShippingAddress { get; private set; }

    /// <summary>
    /// Billing address.
    /// </summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>
    /// Whether billing address is same as shipping.
    /// </summary>
    public bool BillingSameAsShipping { get; private set; }

    // Shipping
    /// <summary>
    /// Selected shipping method.
    /// </summary>
    public string? ShippingMethod { get; private set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal ShippingCost { get; private set; }

    /// <summary>
    /// Estimated delivery date.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryAt { get; private set; }

    // Payment
    /// <summary>
    /// Selected payment method.
    /// </summary>
    public PaymentMethod? PaymentMethod { get; private set; }

    /// <summary>
    /// Payment gateway ID to use.
    /// </summary>
    public Guid? PaymentGatewayId { get; private set; }

    // Totals (snapshot from cart)
    /// <summary>
    /// Subtotal from cart items.
    /// </summary>
    public decimal SubTotal { get; private set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Grand total including shipping and tax.
    /// </summary>
    public decimal GrandTotal { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = "VND";

    // Coupon
    /// <summary>
    /// Applied coupon code.
    /// </summary>
    public string? CouponCode { get; private set; }

    // Customer Notes
    /// <summary>
    /// Customer notes/instructions for the order.
    /// </summary>
    public string? CustomerNotes { get; private set; }

    // Result
    /// <summary>
    /// Created order ID when checkout completes.
    /// </summary>
    public Guid? OrderId { get; private set; }

    /// <summary>
    /// Created order number.
    /// </summary>
    public string? OrderNumber { get; private set; }

    // Navigation
    public virtual Cart.Cart? Cart { get; private set; }
    public virtual Order.Order? Order { get; private set; }

    /// <summary>
    /// Creates a new checkout session.
    /// </summary>
    public static CheckoutSession Create(
        Guid cartId,
        string customerEmail,
        decimal subTotal,
        string currency = "VND",
        string? userId = null,
        string? tenantId = null)
    {
        var session = new CheckoutSession(Guid.NewGuid(), tenantId)
        {
            CartId = cartId,
            UserId = userId,
            CustomerEmail = customerEmail,
            SubTotal = subTotal,
            GrandTotal = subTotal,
            Currency = currency,
            Status = CheckoutSessionStatus.Started,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(DefaultExpirationMinutes),
            LastActivityAt = DateTimeOffset.UtcNow,
            DiscountAmount = 0,
            TaxAmount = 0,
            ShippingCost = 0,
            BillingSameAsShipping = true
        };

        session.AddDomainEvent(new CheckoutSessionCreatedEvent(session.Id, cartId, userId));
        return session;
    }

    /// <summary>
    /// Sets customer information.
    /// </summary>
    public void SetCustomerInfo(string customerName, string customerPhone, string? customerEmail = null)
    {
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        if (!string.IsNullOrEmpty(customerEmail))
        {
            CustomerEmail = customerEmail;
        }
        UpdateActivity();
    }

    /// <summary>
    /// Sets the shipping address.
    /// </summary>
    public void SetShippingAddress(Address address)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        ShippingAddress = address;
        if (BillingSameAsShipping)
        {
            BillingAddress = address;
        }

        if (Status == CheckoutSessionStatus.Started)
        {
            var oldStatus = Status;
            Status = CheckoutSessionStatus.AddressComplete;
            AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        }

        AddDomainEvent(new CheckoutAddressSetEvent(Id, "Shipping"));
        UpdateActivity();
    }

    /// <summary>
    /// Sets the billing address.
    /// </summary>
    public void SetBillingAddress(Address address, bool sameAsShipping)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        BillingSameAsShipping = sameAsShipping;
        BillingAddress = sameAsShipping ? ShippingAddress : address;

        AddDomainEvent(new CheckoutAddressSetEvent(Id, "Billing"));
        UpdateActivity();
    }

    /// <summary>
    /// Selects the shipping method.
    /// </summary>
    public void SelectShippingMethod(
        string shippingMethod,
        decimal shippingCost,
        DateTimeOffset? estimatedDeliveryAt = null)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        if (ShippingAddress is null)
            throw new InvalidOperationException("Shipping address must be set before selecting shipping method");

        ShippingMethod = shippingMethod;
        ShippingCost = shippingCost;
        EstimatedDeliveryAt = estimatedDeliveryAt;
        RecalculateGrandTotal();

        if (Status == CheckoutSessionStatus.AddressComplete)
        {
            var oldStatus = Status;
            Status = CheckoutSessionStatus.ShippingSelected;
            AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        }

        AddDomainEvent(new CheckoutShippingSelectedEvent(Id, shippingMethod, shippingCost));
        UpdateActivity();
    }

    /// <summary>
    /// Selects the payment method.
    /// </summary>
    public void SelectPaymentMethod(PaymentMethod paymentMethod, Guid? paymentGatewayId)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        PaymentMethod = paymentMethod;
        PaymentGatewayId = paymentGatewayId;

        if (Status == CheckoutSessionStatus.ShippingSelected)
        {
            var oldStatus = Status;
            Status = CheckoutSessionStatus.PaymentPending;
            AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        }

        UpdateActivity();
    }

    /// <summary>
    /// Applies a coupon to the checkout.
    /// </summary>
    public void ApplyCoupon(string couponCode, decimal discountAmount)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        CouponCode = couponCode;
        DiscountAmount = discountAmount;
        RecalculateGrandTotal();
        UpdateActivity();
    }

    /// <summary>
    /// Removes applied coupon.
    /// </summary>
    public void RemoveCoupon()
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            throw new InvalidOperationException($"Cannot modify checkout session in {Status} status");

        CouponCode = null;
        DiscountAmount = 0;
        RecalculateGrandTotal();
        UpdateActivity();
    }

    /// <summary>
    /// Sets tax amount.
    /// </summary>
    public void SetTax(decimal taxAmount)
    {
        TaxAmount = taxAmount;
        RecalculateGrandTotal();
        UpdateActivity();
    }

    /// <summary>
    /// Sets customer notes.
    /// </summary>
    public void SetCustomerNotes(string? notes)
    {
        CustomerNotes = notes;
        UpdateActivity();
    }

    /// <summary>
    /// Marks the session as processing payment.
    /// </summary>
    public void MarkAsPaymentProcessing()
    {
        if (Status is not (CheckoutSessionStatus.PaymentPending or CheckoutSessionStatus.ShippingSelected))
            throw new InvalidOperationException($"Cannot start payment processing in {Status} status");

        var oldStatus = Status;
        Status = CheckoutSessionStatus.PaymentProcessing;
        AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        UpdateActivity();
    }

    /// <summary>
    /// Completes the checkout and creates an order.
    /// </summary>
    public void Complete(Guid orderId, string orderNumber)
    {
        if (Status is not (CheckoutSessionStatus.PaymentProcessing or CheckoutSessionStatus.PaymentPending))
            throw new InvalidOperationException($"Cannot complete checkout in {Status} status");

        var oldStatus = Status;
        Status = CheckoutSessionStatus.Completed;
        OrderId = orderId;
        OrderNumber = orderNumber;

        AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new CheckoutCompletedEvent(Id, orderId, orderNumber, GrandTotal));
    }

    /// <summary>
    /// Marks the session as expired.
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired)
            return;

        var oldStatus = Status;
        Status = CheckoutSessionStatus.Expired;

        AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new CheckoutExpiredEvent(Id, CartId));
    }

    /// <summary>
    /// Marks the session as abandoned.
    /// </summary>
    public void MarkAsAbandoned()
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            return;

        var oldStatus = Status;
        Status = CheckoutSessionStatus.Abandoned;

        AddDomainEvent(new CheckoutSessionStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new CheckoutAbandonedEvent(Id, CartId, oldStatus));
    }

    /// <summary>
    /// Extends the session expiration.
    /// </summary>
    public void ExtendExpiration(int minutes = DefaultExpirationMinutes)
    {
        if (Status is CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned)
            return;

        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);
    }

    /// <summary>
    /// Checks if the session has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt &&
                             Status is not (CheckoutSessionStatus.Completed or CheckoutSessionStatus.Expired or CheckoutSessionStatus.Abandoned);

    private void UpdateActivity()
    {
        LastActivityAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(DefaultExpirationMinutes);
    }

    private void RecalculateGrandTotal()
    {
        GrandTotal = SubTotal - DiscountAmount + ShippingCost + TaxAmount;
    }
}
