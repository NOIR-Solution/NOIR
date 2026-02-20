namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Represents a refund against a payment transaction.
/// Supports partial and full refunds with approval workflow.
/// </summary>
public class Refund : TenantAggregateRoot<Guid>
{
    private Refund() : base() { }
    private Refund(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// NOIR-generated refund number.
    /// </summary>
    public string RefundNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Reference to the original payment transaction.
    /// </summary>
    public Guid PaymentTransactionId { get; private set; }

    /// <summary>
    /// Gateway-assigned refund ID.
    /// </summary>
    public string? GatewayRefundId { get; private set; }

    /// <summary>
    /// Refund amount.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// Current refund status.
    /// </summary>
    public RefundStatus Status { get; private set; }

    /// <summary>
    /// Reason for the refund.
    /// </summary>
    public RefundReason Reason { get; private set; }

    /// <summary>
    /// Additional detail about the refund reason.
    /// </summary>
    public string? ReasonDetail { get; private set; }

    /// <summary>
    /// User who requested the refund.
    /// </summary>
    public string? RequestedBy { get; private set; }

    /// <summary>
    /// User who approved the refund.
    /// </summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>
    /// When the refund was processed by the gateway.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// Raw gateway response for the refund.
    /// </summary>
    public string? GatewayResponseJson { get; private set; }

    // Concurrency
    /// <summary>
    /// Row version for optimistic concurrency control.
    /// Prevents silent overwrites when two admins modify the same refund simultaneously.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    // Navigation
    public virtual PaymentTransaction? PaymentTransaction { get; private set; }

    public static Refund Create(
        string refundNumber,
        Guid paymentTransactionId,
        decimal amount,
        string currency,
        RefundReason reason,
        string? reasonDetail,
        string requestedBy,
        string? tenantId = null)
    {
        var refund = new Refund(Guid.NewGuid(), tenantId)
        {
            RefundNumber = refundNumber,
            PaymentTransactionId = paymentTransactionId,
            Amount = amount,
            Currency = currency,
            Reason = reason,
            ReasonDetail = reasonDetail,
            RequestedBy = requestedBy,
            Status = RefundStatus.Pending
        };
        refund.AddDomainEvent(new RefundRequestedEvent(refund.Id, paymentTransactionId, amount, reason));
        return refund;
    }

    public void Approve(string approvedBy)
    {
        ApprovedBy = approvedBy;
        Status = RefundStatus.Approved;
    }

    public void MarkAsProcessing()
    {
        Status = RefundStatus.Processing;
    }

    public void Complete(string gatewayRefundId)
    {
        GatewayRefundId = gatewayRefundId;
        Status = RefundStatus.Completed;
        ProcessedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new RefundCompletedEvent(Id, PaymentTransactionId, Amount));
    }

    public void Reject(string reason)
    {
        Status = RefundStatus.Rejected;
        ReasonDetail = reason;
    }

    public void MarkAsFailed(string gatewayResponse)
    {
        Status = RefundStatus.Failed;
        GatewayResponseJson = gatewayResponse;
    }
}
