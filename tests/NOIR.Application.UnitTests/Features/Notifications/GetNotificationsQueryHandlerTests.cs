namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for GetNotificationsQueryHandler.
/// </summary>
public class GetNotificationsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Notification, Guid>> _repositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly GetNotificationsQueryHandler _handler;

    private const string TestUserId = "user-123";

    public GetNotificationsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Notification, Guid>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationMock = new Mock<ILocalizationService>();

        // Default localization returns key as value
        _localizationMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetNotificationsQueryHandler(
            _repositoryMock.Object,
            _currentUserMock.Object,
            _localizationMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = TestUserId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
    }

    private List<Notification> CreateTestNotifications(int count, bool markSomeAsRead = false)
    {
        var notifications = new List<Notification>();
        for (int i = 0; i < count; i++)
        {
            var notification = Notification.Create(
                userId: TestUserId,
                type: (NotificationType)(i % 4),
                category: (NotificationCategory)(i % 5),
                title: $"Test Title {i}",
                message: $"Test Message {i}",
                tenantId: "default");

            if (markSomeAsRead && i % 2 == 0)
            {
                notification.MarkAsRead();
            }

            notifications.Add(notification);
        }
        return notifications;
    }

    #endregion

    #region Unauthorized Tests

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetNotificationsQuery(1, 10, true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_NoNotifications_ShouldReturnEmptyPage()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetNotificationsQuery(1, 10, true);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithNotifications_ShouldReturnPagedResult()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notifications = CreateTestNotifications(5);
        var query = new GetNotificationsQuery(1, 10, true);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(5);
        result.Value.TotalCount.ShouldBe(5);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notifications = CreateTestNotifications(10);
        var query = new GetNotificationsQuery(1, 3, true);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications.Take(3).ToList());

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalPages.ShouldBe(4); // Ceiling(10/3) = 4
    }

    [Fact]
    public async Task Handle_ShouldMapNotificationPropertiesCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notification = Notification.Create(
            userId: TestUserId,
            type: NotificationType.Success,
            category: NotificationCategory.Security,
            title: "Security Alert",
            message: "Your password was changed",
            tenantId: "default",
            iconClass: "fa-shield",
            actionUrl: "/security/settings");

        var query = new GetNotificationsQuery(1, 10, true);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification> { notification });

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value.Items.First();
        dto.Id.ShouldBe(notification.Id);
        dto.Type.ShouldBe(NotificationType.Success);
        dto.Category.ShouldBe(NotificationCategory.Security);
        dto.Title.ShouldBe("Security Alert");
        dto.Message.ShouldBe("Your password was changed");
        dto.IconClass.ShouldBe("fa-shield");
        dto.ActionUrl.ShouldBe("/security/settings");
        dto.IsRead.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ReadNotification_ShouldMapReadAtCorrectly()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notification = Notification.Create(
            userId: TestUserId,
            type: NotificationType.Info,
            category: NotificationCategory.System,
            title: "Test",
            message: "Test message",
            tenantId: "default");
        notification.MarkAsRead();

        var query = new GetNotificationsQuery(1, 10, true);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification> { notification });

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value.Items.First();
        dto.IsRead.ShouldBe(true);
        dto.ReadAt.ShouldNotBeNull();
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task Handle_IncludeReadFalse_ShouldPassFilterToSpec()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetNotificationsQuery(1, 10, false);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Verify both list and count specs are called (filter is applied via spec)
        _repositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
