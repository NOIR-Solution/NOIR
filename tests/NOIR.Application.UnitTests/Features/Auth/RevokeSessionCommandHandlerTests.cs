namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RevokeSessionCommandHandler.
/// Tests session revocation scenarios with mocked dependencies.
/// </summary>
public class RevokeSessionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly RevokeSessionCommandHandler _handler;

    public RevokeSessionCommandHandlerTests()
    {
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new RevokeSessionCommandHandler(
            _refreshTokenServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = "user-123")
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
    }

    private RefreshToken CreateTestSession(Guid id, string userId, string token)
    {
        var session = RefreshToken.Create(token, userId, 7);
        // Use reflection to set the Id since it's a domain entity
        typeof(RefreshToken).GetProperty("Id")?.SetValue(session, id);
        return session;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidSession_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var sessionId = Guid.NewGuid();
        const string sessionToken = "session-token-123";

        SetupAuthenticatedUser(userId);

        var session = CreateTestSession(sessionId, userId, sessionToken);
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var command = new RevokeSessionCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidSession_ShouldRevokeToken()
    {
        // Arrange
        const string userId = "user-123";
        var sessionId = Guid.NewGuid();
        const string sessionToken = "session-token-123";
        const string ipAddress = "192.168.1.1";

        SetupAuthenticatedUser(userId);

        var session = CreateTestSession(sessionId, userId, sessionToken);
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var command = new RevokeSessionCommand(sessionId, ipAddress);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                sessionToken,
                ipAddress,
                It.Is<string>(r => r.Contains("User requested")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullIpAddress_ShouldStillSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var sessionId = Guid.NewGuid();
        const string sessionToken = "session-token-123";

        SetupAuthenticatedUser(userId);

        var session = CreateTestSession(sessionId, userId, sessionToken);
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var command = new RevokeSessionCommand(sessionId, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(sessionToken, null, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Authentication Failure Scenarios

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var command = new RevokeSessionCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
        _refreshTokenServiceMock.Verify(
            x => x.GetActiveSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);
        var command = new RevokeSessionCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
        var command = new RevokeSessionCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Session Not Found Scenarios

    [Fact]
    public async Task Handle_WithNonExistentSession_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "user-123";
        var nonExistentSessionId = Guid.NewGuid();

        SetupAuthenticatedUser(userId);

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new RevokeSessionCommand(nonExistentSessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Session.NotFound");
    }

    [Fact]
    public async Task Handle_WithDifferentSessionId_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "user-123";
        var existingSessionId = Guid.NewGuid();
        var requestedSessionId = Guid.NewGuid();

        SetupAuthenticatedUser(userId);

        var session = CreateTestSession(existingSessionId, userId, "token");
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var command = new RevokeSessionCommand(requestedSessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Session.NotFound");
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Multiple Sessions Scenarios

    [Fact]
    public async Task Handle_WithMultipleSessions_ShouldRevokeCorrectOne()
    {
        // Arrange
        const string userId = "user-123";
        var session1Id = Guid.NewGuid();
        var session2Id = Guid.NewGuid();
        var session3Id = Guid.NewGuid();
        const string targetToken = "target-token";

        SetupAuthenticatedUser(userId);

        var sessions = new[]
        {
            CreateTestSession(session1Id, userId, "token-1"),
            CreateTestSession(session2Id, userId, targetToken),
            CreateTestSession(session3Id, userId, "token-3")
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var command = new RevokeSessionCommand(session2Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(targetToken, It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";
        var sessionId = Guid.NewGuid();

        SetupAuthenticatedUser(userId);

        var session = CreateTestSession(sessionId, userId, "token");
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var command = new RevokeSessionCommand(sessionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.GetActiveSessionsAsync(userId, token),
            Times.Once);
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotRevokeOtherUsersSessions()
    {
        // Arrange
        const string currentUserId = "user-123";
        const string otherUserId = "user-456";
        var otherUserSessionId = Guid.NewGuid();

        SetupAuthenticatedUser(currentUserId);

        // Return empty sessions for current user (session belongs to different user)
        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new RevokeSessionCommand(otherUserSessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Session.NotFound");
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
