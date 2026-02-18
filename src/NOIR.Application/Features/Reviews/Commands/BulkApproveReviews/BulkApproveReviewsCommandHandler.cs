namespace NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;

/// <summary>
/// Wolverine handler for bulk approving reviews.
/// </summary>
public class BulkApproveReviewsCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewAggregationService _aggregationService;

    public BulkApproveReviewsCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork,
        IReviewAggregationService aggregationService)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _aggregationService = aggregationService;
    }

    public async Task<Result<int>> Handle(
        BulkApproveReviewsCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewsByIdsForUpdateSpec(command.ReviewIds);
        var reviews = await _reviewRepository.ListAsync(spec, cancellationToken);

        if (reviews.Count == 0)
        {
            return Result.Failure<int>(
                Error.NotFound("No reviews found with the specified IDs.", "NOIR-REVIEW-003"));
        }

        var productIds = new HashSet<Guid>();
        foreach (var review in reviews)
        {
            review.Approve();
            productIds.Add(review.ProductId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Recalculate ratings for all affected products
        foreach (var productId in productIds)
        {
            await _aggregationService.RecalculateProductRatingAsync(productId, cancellationToken);
        }

        return Result.Success(reviews.Count);
    }
}
