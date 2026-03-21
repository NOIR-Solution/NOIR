namespace NOIR.Application.Features.Reviews.Queries.GetReviews;

/// <summary>
/// Wolverine handler for getting reviews with moderation filters.
/// </summary>
public class GetReviewsQueryHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUserDisplayNameService _userDisplayNameService;

    public GetReviewsQueryHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IRepository<Product, Guid> productRepository,
        IUserDisplayNameService userDisplayNameService)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _userDisplayNameService = userDisplayNameService;
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
            query.Status, query.ProductId, query.Rating, query.Search, skip, query.PageSize,
            query.OrderBy, query.IsDescending);
        var reviews = await _reviewRepository.ListAsync(listSpec, cancellationToken);

        // Resolve product names (batch query)
        var productIds = reviews.Select(r => r.ProductId).Distinct().ToList();
        var productNames = new Dictionary<Guid, string>();
        if (productIds.Count > 0)
        {
            var products = await _productRepository.ListAsync(
                new ProductsByIdsSpec(productIds), cancellationToken);
            foreach (var p in products)
                productNames[p.Id] = p.Name;
        }

        // Resolve user names (audit + reviewer)
        var userIds = reviews
            .SelectMany(x => new[] { x.CreatedBy, x.ModifiedBy, x.UserId })
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct();
        var userNames = await _userDisplayNameService.GetDisplayNamesAsync(userIds, cancellationToken);

        var items = reviews.Select(r => ReviewMapper.ToDto(
            r,
            productName: productNames.GetValueOrDefault(r.ProductId),
            userName: userNames.GetValueOrDefault(r.UserId),
            userNames: userNames)).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<ReviewDto>.Create(
            items, totalCount, pageIndex, query.PageSize));
    }
}
