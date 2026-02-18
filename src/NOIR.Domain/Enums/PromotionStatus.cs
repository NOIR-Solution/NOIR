namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a promotion through its lifecycle.
/// </summary>
public enum PromotionStatus
{
    /// <summary>
    /// Promotion is being set up, not yet active.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Promotion is currently active and can be applied.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Promotion is scheduled for a future date.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Promotion has passed its end date.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Promotion was manually cancelled.
    /// </summary>
    Cancelled = 4
}
