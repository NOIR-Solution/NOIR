namespace NOIR.Application.Features.Reviews.Commands.VoteReview;

/// <summary>
/// Wolverine handler for voting on a review's helpfulness.
/// </summary>
public class VoteReviewCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public VoteReviewCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result> Handle(
        VoteReviewCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewByIdForUpdateSpec(command.ReviewId);
        var review = await _reviewRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (review is null)
        {
            return Result.Failure(
                Error.NotFound($"Review with ID '{command.ReviewId}' not found.", "NOIR-REVIEW-002"));
        }

        if (command.IsHelpful)
            review.VoteHelpful();
        else
            review.VoteNotHelpful();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Review",
            entityId: review.Id,
            operation: EntityOperation.Updated,
            tenantId: review.TenantId!,
            ct: cancellationToken);

        return Result.Success();
    }
}
