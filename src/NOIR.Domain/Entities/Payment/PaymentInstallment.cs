namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Represents a single installment in a split payment plan.
/// </summary>
public class PaymentInstallment : TenantEntity<Guid>
{
    private PaymentInstallment() { }

    /// <summary>
    /// The parent payment transaction.
    /// </summary>
    public Guid PaymentTransactionId { get; private set; }

    /// <summary>
    /// Installment number (1, 2, 3, etc.).
    /// </summary>
    public int InstallmentNumber { get; private set; }

    /// <summary>
    /// Total number of installments in the plan.
    /// </summary>
    public int TotalInstallments { get; private set; }

    /// <summary>
    /// Amount due for this installment.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// When this installment is due.
    /// </summary>
    public DateTimeOffset DueDate { get; private set; }

    /// <summary>
    /// Current status of the installment.
    /// </summary>
    public InstallmentStatus Status { get; private set; }

    /// <summary>
    /// When the installment was paid.
    /// </summary>
    public DateTimeOffset? PaidAt { get; private set; }

    /// <summary>
    /// Gateway reference for this installment payment.
    /// </summary>
    public string? GatewayReference { get; private set; }

    /// <summary>
    /// Failure reason if payment failed.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Number of payment retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Navigation property to parent transaction.
    /// </summary>
    public virtual PaymentTransaction? PaymentTransaction { get; private set; }

    /// <summary>
    /// Creates a new payment installment.
    /// </summary>
    public static PaymentInstallment Create(
        Guid paymentTransactionId,
        int installmentNumber,
        int totalInstallments,
        decimal amount,
        string currency,
        DateTimeOffset dueDate,
        string? tenantId = null)
    {
        return new PaymentInstallment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentTransactionId = paymentTransactionId,
            InstallmentNumber = installmentNumber,
            TotalInstallments = totalInstallments,
            Amount = amount,
            Currency = currency,
            DueDate = dueDate,
            Status = InstallmentStatus.Scheduled
        };
    }

    public void MarkAsPending()
    {
        Status = InstallmentStatus.Pending;
    }

    public void MarkAsPaid(string gatewayReference)
    {
        Status = InstallmentStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        GatewayReference = gatewayReference;
    }

    public void MarkAsFailed(string reason)
    {
        Status = InstallmentStatus.Failed;
        FailureReason = reason;
        RetryCount++;
    }

    public void Cancel()
    {
        Status = InstallmentStatus.Cancelled;
    }

    public void ResetForRetry()
    {
        Status = InstallmentStatus.Pending;
        FailureReason = null;
    }
}
