namespace NOIR.Application.Features.Reviews.Commands.AddAdminResponse;

/// <summary>
/// Wolverine handler for adding an admin response to a review.
/// </summary>
public class AddAdminResponseCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddAdminResponseCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReviewDto>> Handle(
        AddAdminResponseCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewByIdForUpdateSpec(command.ReviewId);
        var review = await _reviewRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (review is null)
        {
            return Result.Failure<ReviewDto>(
                Error.NotFound($"Review with ID '{command.ReviewId}' not found.", "NOIR-REVIEW-002"));
        }

        review.AddAdminResponse(command.Response);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ReviewMapper.ToDto(review));
    }
}
