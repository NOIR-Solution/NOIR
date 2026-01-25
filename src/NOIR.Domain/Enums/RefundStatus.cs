namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund request pending approval.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Refund approved, awaiting processing.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Refund being processed by gateway.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Refund successfully completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Refund request rejected.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Refund processing failed.
    /// </summary>
    Failed = 5
}
