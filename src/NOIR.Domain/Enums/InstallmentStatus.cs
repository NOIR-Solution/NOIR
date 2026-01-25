namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a payment installment.
/// </summary>
public enum InstallmentStatus
{
    /// <summary>
    /// Installment is scheduled for future payment.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Installment is pending payment.
    /// </summary>
    Pending,

    /// <summary>
    /// Installment has been paid.
    /// </summary>
    Paid,

    /// <summary>
    /// Installment payment failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Installment has been cancelled.
    /// </summary>
    Cancelled
}
