namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for recalculating product review aggregations (average rating, review count).
/// </summary>
public interface IReviewAggregationService
{
    /// <summary>
    /// Recalculates the average rating and review count for a product
    /// based on approved reviews.
    /// </summary>
    Task RecalculateProductRatingAsync(Guid productId, CancellationToken cancellationToken = default);
}
