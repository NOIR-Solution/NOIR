namespace NOIR.Application.UnitTests.Features.Auth;

using NOIR.Application.Features.Auth.Queries.GetActiveSessions;

/// <summary>
/// Unit tests for GetActiveSessionsQueryHandler.
/// Tests all session retrieval scenarios with mocked dependencies.
/// </summary>
public class GetActiveSessionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetActiveSessionsQueryHandler _handler;
    private const string TestUserId = "user-123";
    private const string TestTenantId = "tenant-abc";

    public GetActiveSessionsQueryHandlerTests()
    {
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetActiveSessionsQueryHandler(
            _refreshTokenServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = TestUserId, string tenantId = TestTenantId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
    }

    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private static RefreshToken CreateTestSession(
        string userId,
        string? deviceName = "Test Device",
        string? userAgent = "Test Browser",
        string? ipAddress = "127.0.0.1",
        string? token = null)
    {
        return RefreshToken.Create(
            token: token ?? GenerateTestToken(),
            userId: userId,
            expirationDays: 7,
            tenantId: TestTenantId,
            ipAddress: ipAddress,
            deviceFingerprint: "test-fingerprint",
            userAgent: userAgent,
            deviceName: deviceName);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_AuthenticatedUserWithSessions_ShouldReturnSessions()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetActiveSessionsQuery();

        var sessions = new List<RefreshToken>
        {
            CreateTestSession(TestUserId, "Desktop Chrome", "Chrome/120.0", "192.168.1.1"),
            CreateTestSession(TestUserId, "Mobile Safari", "Safari/17.0", "10.0.0.1")
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WithCurrentRefreshToken_ShouldMarkCurrentSession()
    {
        // Arrange
        SetupAuthenticatedUser();
        var currentToken = GenerateTestToken();
        var query = new GetActiveSessionsQuery(CurrentRefreshToken: currentToken);

        var sessions = new List<RefreshToken>
        {
            CreateTestSession(TestUserId, "Desktop Chrome", token: currentToken),
            CreateTestSession(TestUserId, "Mobile Safari")
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
        result.Value.Where(s => s.IsCurrent);
        result.Value.First().IsCurrent.ShouldBe(true); // Current session should be first (sorted)
    }

    [Fact]
    public async Task Handle_SessionsOrderedByCurrentThenCreatedAt()
    {
        // Arrange
        SetupAuthenticatedUser();
        var currentToken = GenerateTestToken();
        var query = new GetActiveSessionsQuery(CurrentRefreshToken: currentToken);

        var oldSession = CreateTestSession(TestUserId, "Old Device", token: GenerateTestToken());
        var currentSession = CreateTestSession(TestUserId, "Current Device", token: currentToken);
        var newSession = CreateTestSession(TestUserId, "New Device", token: GenerateTestToken());

        var sessions = new List<RefreshToken> { oldSession, currentSession, newSession };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.First().IsCurrent.ShouldBe(true); // Current should be first
        result.Value.First().DeviceName.ShouldBe("Current Device");
    }

    [Fact]
    public async Task Handle_EmptySessionsList_ShouldReturnEmptyList()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetActiveSessionsQuery();

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapAllSessionProperties()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetActiveSessionsQuery();

        var session = CreateTestSession(
            TestUserId,
            deviceName: "Test Device",
            userAgent: "Mozilla/5.0",
            ipAddress: "192.168.1.100");

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken> { session });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var sessionDto = result.Value.Single();
        sessionDto.Id.ShouldBe(session.Id);
        sessionDto.DeviceName.ShouldBe("Test Device");
        sessionDto.UserAgent.ShouldBe("Mozilla/5.0");
        sessionDto.IpAddress.ShouldBe("192.168.1.100");
        sessionDto.CreatedAt.ShouldBe(session.CreatedAt, TimeSpan.FromSeconds(1));
        sessionDto.ExpiresAt.ShouldBe(session.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_NullCurrentRefreshToken_ShouldNotMarkAnyAsCurrent()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetActiveSessionsQuery(CurrentRefreshToken: null);

        var sessions = new List<RefreshToken>
        {
            CreateTestSession(TestUserId, "Device 1"),
            CreateTestSession(TestUserId, "Device 2")
        };

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldAllBe(s => s.IsCurrent == false);
    }

    #endregion

    #region Failure Scenarios - Unauthorized

    [Fact]
    public async Task Handle_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetActiveSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        result.Error.Message.ShouldContain("auth.user.notAuthenticated");
    }

    [Fact]
    public async Task Handle_NullUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        var query = new GetActiveSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);

        var query = new GetActiveSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_NotAuthenticatedWithNonNullUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);

        var query = new GetActiveSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        SetupAuthenticatedUser();
        var query = new GetActiveSessionsQuery();

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(TestUserId, cancellationToken))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.GetActiveSessionsAsync(TestUserId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CancelledToken_ShouldStillCheckAuthentication()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetActiveSessionsQuery();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _handler.Handle(query, cts.Token);

        // Assert - Should fail on authentication check, not cancellation
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldNotCallServiceWhenUnauthenticated()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var query = new GetActiveSessionsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.GetActiveSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallServiceWithCorrectUserId()
    {
        // Arrange
        var specificUserId = "specific-user-456";
        SetupAuthenticatedUser(userId: specificUserId);
        var query = new GetActiveSessionsQuery();

        _refreshTokenServiceMock
            .Setup(x => x.GetActiveSessionsAsync(specificUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefreshToken>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.GetActiveSessionsAsync(specificUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
