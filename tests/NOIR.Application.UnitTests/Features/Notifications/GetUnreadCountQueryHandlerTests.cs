namespace NOIR.Application.UnitTests.Features.Notifications;

/// <summary>
/// Unit tests for GetUnreadCountQueryHandler.
/// </summary>
public class GetUnreadCountQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Notification, Guid>> _repositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly GetUnreadCountQueryHandler _handler;

    public GetUnreadCountQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Notification, Guid>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationMock = new Mock<ILocalizationService>();

        // Default localization returns key as value
        _localizationMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetUnreadCountQueryHandler(
            _repositoryMock.Object,
            _currentUserMock.Object,
            _localizationMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = "user-123")
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
    }

    #endregion

    #region Unauthorized Tests

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetUnreadCountQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnUnreadCount()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetUnreadCountQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ZeroUnread_ShouldReturnZeroCount()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetUnreadCountQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        const string userId = "specific-user-id";
        SetupAuthenticatedUser(userId);
        var query = new GetUnreadCountQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ISpecification<Notification>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
