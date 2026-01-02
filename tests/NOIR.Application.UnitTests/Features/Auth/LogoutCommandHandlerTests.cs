namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for LogoutCommandHandler.
/// Tests logout scenarios including single session and all sessions revocation.
/// </summary>
public class LogoutCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<ICookieAuthService> _cookieAuthServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _cookieAuthServiceMock = new Mock<ICookieAuthService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _currentUserMock = new Mock<ICurrentUser>();

        _handler = new LogoutCommandHandler(
            _cookieAuthServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _currentUserMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldClearCookies()
    {
        // Arrange
        var command = new LogoutCommand();
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(x => x.ClearAuthCookies(), Times.Once);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldClearCookiesAndSucceed()
    {
        // Arrange
        var command = new LogoutCommand();
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(x => x.ClearAuthCookies(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRefreshToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = "test-refresh-token";
        var command = new LogoutCommand(RefreshToken: refreshToken);
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                refreshToken,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutRefreshToken_ShouldGetFromCookie()
    {
        // Arrange
        var cookieToken = "cookie-refresh-token";
        var command = new LogoutCommand();
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");
        _cookieAuthServiceMock.Setup(x => x.GetRefreshTokenFromCookie()).Returns(cookieToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(x => x.GetRefreshTokenFromCookie(), Times.Once);
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                cookieToken,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RevokeAllSessions_ShouldRevokeAllUserTokens()
    {
        // Arrange
        var command = new LogoutCommand(RevokeAllSessions: true);
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(
                "user-123",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RevokeAllSessions_ShouldNotRevokeSingleToken()
    {
        // Arrange
        var command = new LogoutCommand(RefreshToken: "some-token", RevokeAllSessions: true);
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_NoRefreshTokenAvailable_ShouldNotAttemptRevocation()
    {
        // Arrange
        var command = new LogoutCommand();
        _currentUserMock.Setup(x => x.UserId).Returns("user-123");
        _cookieAuthServiceMock.Setup(x => x.GetRefreshTokenFromCookie()).Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ShouldNotRevokeAnyTokens()
    {
        // Arrange
        var command = new LogoutCommand(RefreshToken: "some-token");
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _refreshTokenServiceMock.Verify(
            x => x.RevokeTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AlwaysClearsCookies_EvenWhenNotAuthenticated()
    {
        // Arrange
        var command = new LogoutCommand();
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cookieAuthServiceMock.Verify(x => x.ClearAuthCookies(), Times.Once);
    }

    #endregion
}
