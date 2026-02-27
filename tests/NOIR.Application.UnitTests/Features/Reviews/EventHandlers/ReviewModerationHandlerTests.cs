using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Application.Features.Reviews.EventHandlers;
using NOIR.Domain.Events.Review;

namespace NOIR.Application.UnitTests.Features.Reviews.EventHandlers;

/// <summary>
/// Unit tests for ReviewModerationHandler.
/// Verifies admin moderation alerts and customer approval/rejection email notifications.
/// </summary>
public class ReviewModerationHandlerTests
{
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepository = new();
    private readonly Mock<IUserIdentityService> _userIdentityService = new();
    private readonly Mock<ILogger<ReviewModerationHandler>> _logger = new();
    private readonly ReviewModerationHandler _sut;

    private static NotificationDto MakeNotificationDto() =>
        new(Guid.NewGuid(), NotificationType.Info, NotificationCategory.Workflow,
            "Title", "Message", null, false, null, null,
            Enumerable.Empty<NotificationActionDto>(), DateTimeOffset.UtcNow);

    public ReviewModerationHandlerTests()
    {
        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(1));

        _notificationService
            .Setup(x => x.SendToUserAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(MakeNotificationDto()));

        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new ReviewModerationHandler(
            _notificationService.Object,
            _emailService.Object,
            _reviewRepository.Object,
            _userIdentityService.Object,
            _logger.Object);
    }

    private static UserIdentityDto CreateUserDto(string userId = "user-abc", string email = "reviewer@example.com")
        => new(
            Id: userId,
            Email: email,
            TenantId: "tenant-1",
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);

    #region ReviewCreatedEvent

    [Fact]
    public async Task Handle_ReviewCreated_ShouldSendAdminModerationAlert()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var evt = new ReviewCreatedEvent(reviewId, productId, "user-123", 5);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Info,
            NotificationCategory.Workflow,
            "New Review Pending Moderation",
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.Is<string?>(u => u != null && u.Contains(reviewId.ToString())),
            It.IsAny<IEnumerable<NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReviewCreated_WhenNotificationThrows_ShouldLogWarningAndNotRethrow()
    {
        // Arrange
        var evt = new ReviewCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), "user-err", 3);
        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification hub down"));

        // Act & Assert
        await ((Func<Task>)(() => _sut.Handle(evt, CancellationToken.None))).Should().NotThrowAsync();
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region ReviewApprovedEvent

    [Fact]
    public async Task Handle_ReviewApproved_WhenReviewAndUserExist_ShouldSendApprovalEmail()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = "user-approve";
        var evt = new ReviewApprovedEvent(reviewId, productId);

        var review = ProductReview.Create(productId, userId, 4, "Great", "Excellent product", tenantId: null);
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var user = CreateUserDto(userId, "reviewer@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "reviewer@example.com",
            "Your Review Has Been Approved",
            "review_approved",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReviewApproved_WhenReviewAndUserExist_ShouldSendInAppNotificationToUser()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = "user-inapp";
        var evt = new ReviewApprovedEvent(reviewId, productId);

        var review = ProductReview.Create(productId, userId, 5, "Amazing", "Loved it", tenantId: null);
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var user = CreateUserDto(userId, "inapp@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToUserAsync(
            userId,
            NotificationType.Success,
            NotificationCategory.Workflow,
            "Review Approved",
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReviewApproved_WhenReviewNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var evt = new ReviewApprovedEvent(reviewId, Guid.NewGuid());
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ReviewApproved_WhenUserNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = "user-ghost";
        var evt = new ReviewApprovedEvent(reviewId, productId);

        var review = ProductReview.Create(productId, userId, 3, null, "Decent product", tenantId: null);
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ReviewRejectedEvent

    [Fact]
    public async Task Handle_ReviewRejected_WhenReviewAndUserExist_ShouldSendRejectionEmail()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = "user-reject";
        var evt = new ReviewRejectedEvent(reviewId, productId);

        var review = ProductReview.Create(productId, userId, 1, "Bad", "Terrible product", tenantId: null);
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var user = CreateUserDto(userId, "reject@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "reject@example.com",
            "Your Review Was Not Approved",
            "review_rejected",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReviewRejected_WhenReviewAndUserExist_ShouldSendInAppWarningNotificationToUser()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = "user-reject-inapp";
        var evt = new ReviewRejectedEvent(reviewId, productId);

        var review = ProductReview.Create(productId, userId, 2, null, "Misleading content", tenantId: null);
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var user = CreateUserDto(userId, "warn@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToUserAsync(
            userId,
            NotificationType.Warning,
            NotificationCategory.Workflow,
            "Review Not Approved",
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReviewRejected_WhenReviewNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var evt = new ReviewRejectedEvent(reviewId, Guid.NewGuid());
        _reviewRepository.Setup(x => x.GetByIdAsync(reviewId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
