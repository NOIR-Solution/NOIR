namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for MarkAsReadCommandHandler.
/// </summary>
public class MarkAsReadCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Notification, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<INotificationHubContext> _hubContextMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly MarkAsReadCommandHandler _handler;

    private static readonly Guid TestNotificationId = Guid.NewGuid();
    private const string TestUserId = "user-123";

    public MarkAsReadCommandHandlerTests()
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

        _handler = new MarkAsReadCommandHandler(
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

    private Notification CreateTestNotification(bool isRead = false)
    {
        var notification = Notification.Create(
            userId: TestUserId,
            type: NotificationType.Info,
            category: NotificationCategory.System,
            title: "Test Title",
            message: "Test Message",
            tenantId: "default");

        if (isRead)
        {
            notification.MarkAsRead();
        }

        return notification;
    }

    #endregion

    #region Unauthorized Tests

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var command = new MarkAsReadCommand(TestNotificationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_NotificationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new MarkAsReadCommand(TestNotificationId);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("NOT_FOUND");
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_ValidRequest_ShouldMarkNotificationAsRead()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notification = CreateTestNotification(isRead: false);
        var command = new MarkAsReadCommand(notification.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldSaveChanges()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notification = CreateTestNotification(isRead: false);
        var command = new MarkAsReadCommand(notification.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateUnreadCountViaSignalR()
    {
        // Arrange
        SetupAuthenticatedUser();
        var notification = CreateTestNotification(isRead: false);
        var command = new MarkAsReadCommand(notification.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _hubContextMock.Verify(
            x => x.UpdateUnreadCountAsync(TestUserId, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
