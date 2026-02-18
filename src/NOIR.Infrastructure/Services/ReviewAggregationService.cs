namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for recalculating product review aggregations.
/// Updates the ProductFilterIndex with average rating and review count.
/// </summary>
public class ReviewAggregationService : IReviewAggregationService, IScopedService
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly ApplicationDbContext _dbContext;

    public ReviewAggregationService(
        IRepository<ProductReview, Guid> reviewRepository,
        ApplicationDbContext dbContext)
    {
        _reviewRepository = reviewRepository;
        _dbContext = dbContext;
    }

    public async Task RecalculateProductRatingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Get all approved reviews for the product
        var spec = new NOIR.Application.Features.Reviews.Specifications.ReviewStatsSpec(productId);
        var reviews = await _reviewRepository.ListAsync(spec, cancellationToken);

        var totalReviews = reviews.Count;
        var averageRating = totalReviews > 0
            ? Math.Round((decimal)reviews.Sum(r => r.Rating) / totalReviews, 1)
            : (decimal?)null;

        // Update the ProductFilterIndex
        var filterIndex = await _dbContext.ProductFilterIndexes
            .FirstOrDefaultAsync(f => f.ProductId == productId, cancellationToken);

        if (filterIndex is not null)
        {
            filterIndex.UpdateRating(averageRating, totalReviews);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
