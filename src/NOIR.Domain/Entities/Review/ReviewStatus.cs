namespace NOIR.Domain.Entities.Review;

/// <summary>
/// Status of a product review in the moderation workflow.
/// </summary>
public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}
