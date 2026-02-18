namespace NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;

/// <summary>
/// Wolverine handler for bulk rejecting reviews.
/// </summary>
public class BulkRejectReviewsCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewAggregationService _aggregationService;

    public BulkRejectReviewsCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork,
        IReviewAggregationService aggregationService)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _aggregationService = aggregationService;
    }

    public async Task<Result<int>> Handle(
        BulkRejectReviewsCommand command,
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
            review.Reject();
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
