namespace NOIR.Application.Features.Reviews.EventHandlers;

/// <summary>
/// Handles review domain events for moderation notifications.
/// </summary>
public class ReviewModerationHandler
{
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IRepository<ProductReview, Guid> _reviewRepository;
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILogger<ReviewModerationHandler> _logger;

    public ReviewModerationHandler(
        INotificationService notificationService,
        IEmailService emailService,
        IRepository<ProductReview, Guid> reviewRepository,
        IUserIdentityService userIdentityService,
        ILogger<ReviewModerationHandler> logger)
    {
        _notificationService = notificationService;
        _emailService = emailService;
        _reviewRepository = reviewRepository;
        _userIdentityService = userIdentityService;
        _logger = logger;
    }

    public async Task Handle(ReviewCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Notifying admin of new review {ReviewId} for product {ProductId}", evt.ReviewId, evt.ProductId);

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Info,
                NotificationCategory.Workflow,
                "New Review Pending Moderation",
                $"A new {evt.Rating}-star review has been submitted and is pending moderation.",
                actionUrl: $"/reviews/{evt.ReviewId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send moderation notification for review {ReviewId}", evt.ReviewId);
        }
    }

    public async Task Handle(ReviewApprovedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending review approved notification for review {ReviewId}", evt.ReviewId);

        var review = await _reviewRepository.GetByIdAsync(evt.ReviewId, ct);
        if (review is null)
        {
            _logger.LogWarning("Review {ReviewId} not found for approval notification", evt.ReviewId);
            return;
        }

        var user = await _userIdentityService.FindByIdAsync(review.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for review approval notification", review.UserId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                user.Email,
                "Your Review Has Been Approved",
                "review_approved",
                new { ReviewId = evt.ReviewId, ProductId = evt.ProductId },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send review approved email for review {ReviewId}", evt.ReviewId);
        }

        try
        {
            await _notificationService.SendToUserAsync(
                review.UserId,
                NotificationType.Success,
                NotificationCategory.Workflow,
                "Review Approved",
                "Your product review has been approved and is now visible.",
                actionUrl: $"/products/{evt.ProductId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for approved review {ReviewId}", evt.ReviewId);
        }
    }

    public async Task Handle(ReviewRejectedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending review rejected notification for review {ReviewId}", evt.ReviewId);

        var review = await _reviewRepository.GetByIdAsync(evt.ReviewId, ct);
        if (review is null)
        {
            _logger.LogWarning("Review {ReviewId} not found for rejection notification", evt.ReviewId);
            return;
        }

        var user = await _userIdentityService.FindByIdAsync(review.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for review rejection notification", review.UserId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                user.Email,
                "Your Review Was Not Approved",
                "review_rejected",
                new { ReviewId = evt.ReviewId, ProductId = evt.ProductId },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send review rejected email for review {ReviewId}", evt.ReviewId);
        }

        try
        {
            await _notificationService.SendToUserAsync(
                review.UserId,
                NotificationType.Warning,
                NotificationCategory.Workflow,
                "Review Not Approved",
                "Your product review did not meet our guidelines and was not approved.",
                actionUrl: $"/products/{evt.ProductId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for rejected review {ReviewId}", evt.ReviewId);
        }
    }
}
