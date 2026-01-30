namespace NOIR.Domain.Entities.Shipping;

/// <summary>
/// Represents a shipping order created with a provider.
/// Links to a NOIR Order and tracks the shipment lifecycle.
/// </summary>
public class ShippingOrder : TenantAggregateRoot<Guid>
{
    private readonly List<ShippingTrackingEvent> _trackingEvents = new();

    private ShippingOrder() : base() { }
    private ShippingOrder(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// The NOIR order this shipment is for.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The shipping provider used.
    /// </summary>
    public Guid ProviderId { get; private set; }

    /// <summary>
    /// Provider code for quick lookups.
    /// </summary>
    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>
    /// Provider's internal order ID.
    /// </summary>
    public string? ProviderOrderId { get; private set; }

    /// <summary>
    /// Tracking number from the provider.
    /// </summary>
    public string TrackingNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Service type code (e.g., "EXPRESS", "STANDARD").
    /// </summary>
    public string ServiceTypeCode { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable service type name.
    /// </summary>
    public string ServiceTypeName { get; private set; } = string.Empty;

    /// <summary>
    /// Current shipping status.
    /// </summary>
    public ShippingStatus Status { get; private set; }

    /// <summary>
    /// Base shipping rate.
    /// </summary>
    public decimal BaseRate { get; private set; }

    /// <summary>
    /// COD (Cash on Delivery) fee.
    /// </summary>
    public decimal CodFee { get; private set; }

    /// <summary>
    /// Insurance fee.
    /// </summary>
    public decimal InsuranceFee { get; private set; }

    /// <summary>
    /// Total shipping fee (BaseRate + CodFee + InsuranceFee).
    /// </summary>
    public decimal TotalShippingFee { get; private set; }

    /// <summary>
    /// COD amount to collect from recipient (null if not COD).
    /// </summary>
    public decimal? CodAmount { get; private set; }

    /// <summary>
    /// Declared value of the shipment for insurance.
    /// </summary>
    public decimal DeclaredValue { get; private set; }

    /// <summary>
    /// Total weight in grams.
    /// </summary>
    public decimal WeightGrams { get; private set; }

    /// <summary>
    /// Pickup address as JSON.
    /// </summary>
    public string PickupAddressJson { get; private set; } = "{}";

    /// <summary>
    /// Delivery address as JSON.
    /// </summary>
    public string DeliveryAddressJson { get; private set; } = "{}";

    /// <summary>
    /// Sender contact info as JSON.
    /// </summary>
    public string SenderJson { get; private set; } = "{}";

    /// <summary>
    /// Recipient contact info as JSON.
    /// </summary>
    public string RecipientJson { get; private set; } = "{}";

    /// <summary>
    /// Items being shipped as JSON array.
    /// </summary>
    public string ItemsJson { get; private set; } = "[]";

    /// <summary>
    /// URL to shipping label (if available).
    /// </summary>
    public string? LabelUrl { get; private set; }

    /// <summary>
    /// URL to track the shipment on provider's website.
    /// </summary>
    public string? TrackingUrl { get; private set; }

    /// <summary>
    /// Estimated delivery date from provider.
    /// </summary>
    public DateTimeOffset? EstimatedDeliveryDate { get; private set; }

    /// <summary>
    /// Actual delivery date when delivered.
    /// </summary>
    public DateTimeOffset? ActualDeliveryDate { get; private set; }

    /// <summary>
    /// Date/time when picked up from sender.
    /// </summary>
    public DateTimeOffset? PickedUpAt { get; private set; }

    /// <summary>
    /// Additional notes for the shipment.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Provider's raw response when creating order (for debugging).
    /// </summary>
    public string? ProviderRawResponse { get; private set; }

    /// <summary>
    /// Whether shipping is paid by merchant (true) or customer (false).
    /// </summary>
    public bool IsFreeship { get; private set; }

    /// <summary>
    /// Tracking events for this shipment.
    /// </summary>
    public IReadOnlyCollection<ShippingTrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();

    /// <summary>
    /// Navigation property to provider.
    /// </summary>
    public ShippingProvider? Provider { get; private set; }

    public static ShippingOrder Create(
        Guid orderId,
        Guid providerId,
        ShippingProviderCode providerCode,
        string serviceTypeCode,
        string serviceTypeName,
        string pickupAddressJson,
        string deliveryAddressJson,
        string senderJson,
        string recipientJson,
        string itemsJson,
        decimal weightGrams,
        decimal declaredValue,
        decimal? codAmount,
        bool isFreeship,
        string? notes,
        string? tenantId = null)
    {
        var order = new ShippingOrder(Guid.NewGuid(), tenantId)
        {
            OrderId = orderId,
            ProviderId = providerId,
            ProviderCode = providerCode,
            ServiceTypeCode = serviceTypeCode,
            ServiceTypeName = serviceTypeName,
            Status = ShippingStatus.Draft,
            PickupAddressJson = pickupAddressJson,
            DeliveryAddressJson = deliveryAddressJson,
            SenderJson = senderJson,
            RecipientJson = recipientJson,
            ItemsJson = itemsJson,
            WeightGrams = weightGrams,
            DeclaredValue = declaredValue,
            CodAmount = codAmount,
            IsFreeship = isFreeship,
            Notes = notes
        };

        order.AddDomainEvent(new ShippingOrderCreatedEvent(order.Id, orderId, providerCode));
        return order;
    }

    public void SetProviderResponse(
        string trackingNumber,
        string? providerOrderId,
        string? labelUrl,
        string? trackingUrl,
        decimal baseRate,
        decimal codFee,
        decimal insuranceFee,
        DateTimeOffset? estimatedDeliveryDate,
        string? rawResponse)
    {
        TrackingNumber = trackingNumber;
        ProviderOrderId = providerOrderId;
        LabelUrl = labelUrl;
        TrackingUrl = trackingUrl;
        BaseRate = baseRate;
        CodFee = codFee;
        InsuranceFee = insuranceFee;
        TotalShippingFee = baseRate + codFee + insuranceFee;
        EstimatedDeliveryDate = estimatedDeliveryDate;
        ProviderRawResponse = rawResponse;
        Status = ShippingStatus.AwaitingPickup;

        AddDomainEvent(new ShippingOrderSubmittedEvent(Id, TrackingNumber, ProviderCode));
    }

    public void UpdateStatus(ShippingStatus newStatus, string? location = null)
    {
        var previousStatus = Status;
        Status = newStatus;

        switch (newStatus)
        {
            case ShippingStatus.PickedUp:
                PickedUpAt = DateTimeOffset.UtcNow;
                break;
            case ShippingStatus.Delivered:
                ActualDeliveryDate = DateTimeOffset.UtcNow;
                break;
        }

        AddDomainEvent(new ShippingOrderStatusChangedEvent(
            Id, TrackingNumber, previousStatus, newStatus, location));
    }

    public void AddTrackingEvent(ShippingTrackingEvent trackingEvent)
    {
        _trackingEvents.Add(trackingEvent);

        // Update status if the event represents a status change
        if (trackingEvent.Status != Status)
        {
            UpdateStatus(trackingEvent.Status, trackingEvent.Location);
        }
    }

    public void Cancel(string? reason = null)
    {
        if (Status == ShippingStatus.Delivered || Status == ShippingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel shipping order in status {Status}");
        }

        var previousStatus = Status;
        Status = ShippingStatus.Cancelled;
        Notes = string.IsNullOrEmpty(Notes)
            ? $"Cancelled: {reason}"
            : $"{Notes}\nCancelled: {reason}";

        AddDomainEvent(new ShippingOrderCancelledEvent(Id, TrackingNumber, previousStatus, reason));
    }

    public void SetEstimatedDeliveryDate(DateTimeOffset? date)
    {
        EstimatedDeliveryDate = date;
    }
}
