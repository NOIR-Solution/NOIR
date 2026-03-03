namespace NOIR.Application.Features.Reviews.Commands.ApproveReview;

/// <summary>
/// Wolverine handler for approving a product review.
/// </summary>
public class ApproveReviewCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewAggregationService _aggregationService;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public ApproveReviewCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork,
        IReviewAggregationService aggregationService,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _aggregationService = aggregationService;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<ReviewDto>> Handle(
        ApproveReviewCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewByIdForUpdateSpec(command.ReviewId);
        var review = await _reviewRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (review is null)
        {
            return Result.Failure<ReviewDto>(
                Error.NotFound($"Review with ID '{command.ReviewId}' not found.", "NOIR-REVIEW-002"));
        }

        review.Approve();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Recalculate product rating after approval
        await _aggregationService.RecalculateProductRatingAsync(review.ProductId, cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Review",
            entityId: review.Id,
            operation: EntityOperation.Updated,
            tenantId: review.TenantId!,
            ct: cancellationToken);

        return Result.Success(ReviewMapper.ToDto(review));
    }
}
