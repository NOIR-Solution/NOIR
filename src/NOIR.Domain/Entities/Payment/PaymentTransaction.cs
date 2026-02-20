namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Represents a payment transaction against a gateway.
/// Tracks lifecycle from creation through completion/failure.
/// </summary>
public class PaymentTransaction : TenantAggregateRoot<Guid>
{
    private PaymentTransaction() : base() { }
    private PaymentTransaction(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// NOIR-generated transaction number.
    /// </summary>
    public string TransactionNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Transaction ID assigned by the gateway.
    /// </summary>
    public string? GatewayTransactionId { get; private set; }

    /// <summary>
    /// Reference to the gateway configuration used.
    /// </summary>
    public Guid PaymentGatewayId { get; private set; }

    /// <summary>
    /// Provider name (denormalized for quick queries).
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Associated order ID (nullable until Phase 8).
    /// </summary>
    public Guid? OrderId { get; private set; }

    /// <summary>
    /// Customer who initiated the payment.
    /// </summary>
    public Guid? CustomerId { get; private set; }

    // Financial
    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code (default: VND).
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// Exchange rate at time of transaction.
    /// </summary>
    public decimal? ExchangeRate { get; private set; }

    /// <summary>
    /// Fee charged by the gateway.
    /// </summary>
    public decimal? GatewayFee { get; private set; }

    /// <summary>
    /// Net amount after gateway fees.
    /// </summary>
    public decimal? NetAmount { get; private set; }

    // Status
    /// <summary>
    /// Current payment status.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Reason for failure if applicable.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Gateway-specific failure code.
    /// </summary>
    public string? FailureCode { get; private set; }

    // Details
    /// <summary>
    /// Payment method used.
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// Additional payment method details (e.g., card last 4 digits).
    /// </summary>
    public string? PaymentMethodDetail { get; private set; }

    /// <summary>
    /// Information about the payer.
    /// </summary>
    public string? PayerInfo { get; private set; }

    // Metadata
    /// <summary>
    /// IP address of the initiating request.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent of the initiating request.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Return URL after payment completion.
    /// </summary>
    public string? ReturnUrl { get; private set; }

    /// <summary>
    /// Raw gateway response stored as JSON.
    /// </summary>
    public string? GatewayResponseJson { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? MetadataJson { get; private set; }

    // Timing
    /// <summary>
    /// When the payment was successfully completed.
    /// </summary>
    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>
    /// When the payment link expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    // COD-specific
    /// <summary>
    /// Name of the person who collected the COD payment.
    /// </summary>
    public string? CodCollectorName { get; private set; }

    /// <summary>
    /// When COD payment was collected.
    /// </summary>
    public DateTimeOffset? CodCollectedAt { get; private set; }

    // Idempotency
    /// <summary>
    /// Idempotency key to prevent duplicate processing.
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    // Concurrency
    /// <summary>
    /// Row version for optimistic concurrency control.
    /// Prevents silent overwrites when two admins modify the same payment simultaneously.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // Navigation
    public virtual PaymentGateway? Gateway { get; private set; }
    public virtual ICollection<Refund> Refunds { get; private set; } = new List<Refund>();
    public virtual ICollection<PaymentInstallment> Installments { get; private set; } = new List<PaymentInstallment>();

    public static PaymentTransaction Create(
        string transactionNumber,
        Guid paymentGatewayId,
        string provider,
        decimal amount,
        string currency,
        PaymentMethod paymentMethod,
        string idempotencyKey,
        string? tenantId = null)
    {
        var transaction = new PaymentTransaction(Guid.NewGuid(), tenantId)
        {
            TransactionNumber = transactionNumber,
            PaymentGatewayId = paymentGatewayId,
            Provider = provider,
            Amount = amount,
            Currency = currency,
            PaymentMethod = paymentMethod,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey
        };
        transaction.AddDomainEvent(new PaymentCreatedEvent(
            transaction.Id, transactionNumber, amount, currency, provider));
        return transaction;
    }

    public void SetOrderId(Guid orderId)
    {
        OrderId = orderId;
    }

    public void SetCustomerId(Guid customerId)
    {
        CustomerId = customerId;
    }

    public void SetRequestMetadata(string? ipAddress, string? userAgent, string? returnUrl)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ReturnUrl = returnUrl;
    }

    public void SetExpiresAt(DateTimeOffset expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    public void MarkAsProcessing()
    {
        var oldStatus = Status;
        Status = PaymentStatus.Processing;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsRequiresAction()
    {
        var oldStatus = Status;
        Status = PaymentStatus.RequiresAction;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsPaid(string gatewayTransactionId)
    {
        var oldStatus = Status;
        Status = PaymentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        GatewayTransactionId = gatewayTransactionId;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new PaymentSucceededEvent(Id, Provider, Amount, gatewayTransactionId));
    }

    public void MarkAsFailed(string reason, string? failureCode = null)
    {
        var oldStatus = Status;
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailureCode = failureCode;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status, reason));
        AddDomainEvent(new PaymentFailedEvent(Id, reason, failureCode));
    }

    public void MarkAsCancelled()
    {
        var oldStatus = Status;
        Status = PaymentStatus.Cancelled;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsExpired()
    {
        var oldStatus = Status;
        Status = PaymentStatus.Expired;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void SetGatewayResponse(string gatewayResponseJson)
    {
        GatewayResponseJson = gatewayResponseJson;
    }

    public void SetGatewayFee(decimal gatewayFee)
    {
        GatewayFee = gatewayFee;
        NetAmount = Amount - gatewayFee;
    }

    public void SetGatewayTransactionId(string gatewayTransactionId)
    {
        GatewayTransactionId = gatewayTransactionId;
    }

    public void SetMetadataJson(string metadataJson)
    {
        MetadataJson = metadataJson;
    }

    public void MarkAsCodPending()
    {
        var oldStatus = Status;
        Status = PaymentStatus.CodPending;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsAuthorized()
    {
        var oldStatus = Status;
        Status = PaymentStatus.Authorized;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsRefunded()
    {
        var oldStatus = Status;
        Status = PaymentStatus.Refunded;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
    }

    public void ConfirmCodCollection(string collectorName)
    {
        if (PaymentMethod != PaymentMethod.COD)
            throw new InvalidOperationException("Only COD payments can be confirmed for collection");

        var oldStatus = Status;
        Status = PaymentStatus.CodCollected;
        CodCollectorName = collectorName;
        CodCollectedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new PaymentStatusChangedEvent(Id, oldStatus, Status));
        AddDomainEvent(new CodCollectedEvent(Id, collectorName, CodCollectedAt.Value));
    }
}
