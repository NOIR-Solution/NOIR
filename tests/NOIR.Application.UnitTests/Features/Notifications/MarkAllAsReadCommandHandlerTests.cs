namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for MarkAllAsReadCommandHandler.
/// </summary>
public class MarkAllAsReadCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Notification, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<INotificationHubContext> _hubContextMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly MarkAllAsReadCommandHandler _handler;

    private const string TestUserId = "user-123";

    public MarkAllAsReadCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Notification, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _hubContextMock = new Mock<INotificationHubContext>();
        _localizationMock = new Mock<ILocalizationService>();

        // Default localization returns key as value
        _localizationMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new MarkAllAsReadCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _hubContextMock.Object,
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

    private List<Notification> CreateTestNotifications(int count)
    {
        var notifications = new List<Notification>();
        for (int i = 0; i < count; i++)
        {
            notifications.Add(Notification.Create(
                userId: TestUserId,
                type: NotificationType.Info,
                category: NotificationCategory.System,
                title: $"Test Title {i}",
                message: $"Test Message {i}",
                tenantId: "default"));
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
        var command = new MarkAllAsReadCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_NoUnreadNotifications_ShouldReturnSuccessWithZeroCount()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_HasUnreadNotifications_ShouldMarkAllAsRead()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();
        var notifications = CreateTestNotifications(3);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
        notifications.Should().AllSatisfy(n => n.ReadAt.Should().NotBeNull());
    }

    [Fact]
    public async Task Handle_HasUnreadNotifications_ShouldSaveChanges()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();
        var notifications = CreateTestNotifications(2);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoUnreadNotifications_ShouldNotSaveChanges()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HasUnreadNotifications_ShouldUpdateUnreadCountToZero()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();
        var notifications = CreateTestNotifications(3);

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _hubContextMock.Verify(
            x => x.UpdateUnreadCountAsync(TestUserId, 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoUnreadNotifications_ShouldNotUpdateUnreadCount()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAllAsReadCommand();

        _repositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _hubContextMock.Verify(
            x => x.UpdateUnreadCountAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
