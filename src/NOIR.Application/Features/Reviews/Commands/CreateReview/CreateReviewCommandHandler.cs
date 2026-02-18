namespace NOIR.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Wolverine handler for creating a new product review.
/// </summary>
public class CreateReviewCommandHandler
{
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateReviewCommandHandler(
        IRepository<ProductReview, Guid> reviewRepository,
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var userId = command.UserId ?? _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<ReviewDto>(
                Error.Unauthorized("User must be authenticated to create a review."));
        }

        // Check if user already reviewed this product
        var existsSpec = new ReviewExistsSpec(command.ProductId, userId);
        var existing = await _reviewRepository.FirstOrDefaultAsync(existsSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<ReviewDto>(
                Error.Conflict("You have already reviewed this product.", "NOIR-REVIEW-001"));
        }

        // Check if order is a verified purchase (delivered or completed)
        var isVerifiedPurchase = false;
        if (command.OrderId.HasValue)
        {
            var orderSpec = new OrderByIdSpec(command.OrderId.Value);
            var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);
            if (order is not null &&
                order.Status is OrderStatus.Delivered or OrderStatus.Completed &&
                order.Items.Any(i => i.ProductId == command.ProductId))
            {
                isVerifiedPurchase = true;
            }
        }

        var review = ProductReview.Create(
            command.ProductId,
            userId,
            command.Rating,
            command.Title,
            command.Content,
            command.OrderId,
            isVerifiedPurchase,
            _currentUser.TenantId);

        // Add media
        if (command.MediaUrls?.Any() == true)
        {
            for (var i = 0; i < command.MediaUrls.Count; i++)
            {
                review.AddMedia(command.MediaUrls[i], ReviewMediaType.Image, i);
            }
        }

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ReviewMapper.ToDto(review));
    }
}
