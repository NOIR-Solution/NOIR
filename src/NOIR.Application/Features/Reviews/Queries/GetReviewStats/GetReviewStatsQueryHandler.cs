namespace NOIR.Application.Features.Reviews.Queries.GetReviewStats;

/// <summary>
/// Wolverine handler for getting review statistics for a product.
/// </summary>
public class GetReviewStatsQueryHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;

    public GetReviewStatsQueryHandler(IRepository<ProductReview, Guid> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<ReviewStatsDto>> Handle(
        GetReviewStatsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewStatsSpec(query.ProductId);
        var reviews = await _reviewRepository.ListAsync(spec, cancellationToken);

        var totalReviews = reviews.Count;
        var averageRating = totalReviews > 0
            ? Math.Round((decimal)reviews.Sum(r => r.Rating) / totalReviews, 1)
            : 0m;

        var ratingDistribution = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
        };

        foreach (var review in reviews)
        {
            ratingDistribution[review.Rating]++;
        }

        return Result.Success(new ReviewStatsDto
        {
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            RatingDistribution = ratingDistribution
        });
    }
}
