namespace NOIR.Application.Features.Reviews.Queries.GetProductReviews;

/// <summary>
/// Wolverine handler for getting product reviews (public, approved only).
/// </summary>
public class GetProductReviewsQueryHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;

    public GetProductReviewsQueryHandler(IRepository<ProductReview, Guid> reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetProductReviewsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new ReviewsByProductCountSpec(query.ProductId);
        var totalCount = await _reviewRepository.CountAsync(countSpec, cancellationToken);

        // Get reviews
        var listSpec = new ReviewsByProductSpec(
            query.ProductId, query.Sort, skip, query.PageSize);
        var reviews = await _reviewRepository.ListAsync(listSpec, cancellationToken);

        var items = reviews.Select(r => ReviewMapper.ToDto(r)).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<ReviewDto>.Create(
            items, totalCount, pageIndex, query.PageSize));
    }
}
