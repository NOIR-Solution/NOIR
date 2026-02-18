namespace NOIR.Application.Features.Reviews.Queries.GetReviewById;

/// <summary>
/// Wolverine handler for getting a review by ID.
/// </summary>
public class GetReviewByIdQueryHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;

    public GetReviewByIdQueryHandler(IRepository<ProductReview, Guid> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<ReviewDetailDto>> Handle(
        GetReviewByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new ReviewByIdSpec(query.Id);
        var review = await _reviewRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (review is null)
        {
            return Result.Failure<ReviewDetailDto>(
                Error.NotFound($"Review with ID '{query.Id}' not found.", "NOIR-REVIEW-002"));
        }

        return Result.Success(ReviewMapper.ToDetailDto(review));
    }
}
