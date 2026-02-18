namespace NOIR.Application.Features.Reviews.Queries.GetReviews;

/// <summary>
/// Wolverine handler for getting reviews with moderation filters.
/// </summary>
public class GetReviewsQueryHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;

    public GetReviewsQueryHandler(IRepository<ProductReview, Guid> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetReviewsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new ReviewsModerationCountSpec(
            query.Status, query.ProductId, query.Rating, query.Search);
        var totalCount = await _reviewRepository.CountAsync(countSpec, cancellationToken);

        // Get reviews
        var listSpec = new ReviewsModerationListSpec(
            query.Status, query.ProductId, query.Rating, query.Search, skip, query.PageSize);
        var reviews = await _reviewRepository.ListAsync(listSpec, cancellationToken);

        var items = reviews.Select(r => ReviewMapper.ToDto(r)).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<ReviewDto>.Create(
            items, totalCount, pageIndex, query.PageSize));
    }
}
