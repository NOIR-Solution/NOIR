namespace NOIR.Application.Features.Reviews.Commands.RejectReview;

/// <summary>
/// Wolverine handler for rejecting a product review.
/// </summary>
public class RejectReviewCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewAggregationService _aggregationService;

    public RejectReviewCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork,
        IReviewAggregationService aggregationService)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _aggregationService = aggregationService;
    }

    public async Task<Result<ReviewDto>> Handle(
        RejectReviewCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewByIdForUpdateSpec(command.ReviewId);
        var review = await _reviewRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (review is null)
        {
            return Result.Failure<ReviewDto>(
                Error.NotFound($"Review with ID '{command.ReviewId}' not found.", "NOIR-REVIEW-002"));
        }

        review.Reject();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Recalculate product rating after rejection (in case a previously approved review is rejected)
        await _aggregationService.RecalculateProductRatingAsync(review.ProductId, cancellationToken);

        return Result.Success(ReviewMapper.ToDto(review));
    }
}
