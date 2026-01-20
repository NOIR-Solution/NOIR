namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RefreshTokenCommandHandler.
/// Tests token rotation with security validations and theft detection.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IDeviceFingerprintService> _deviceFingerprintServiceMock;
    private readonly Mock<ICookieAuthService> _cookieAuthServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<RefreshTokenCommandHandler>> _loggerMock;
    private readonly RefreshTokenCommandHandler _handler;
    private const string TestTenantId = "tenant-abc";

    public RefreshTokenCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _deviceFingerprintServiceMock = new Mock<IDeviceFingerprintService>();
        _cookieAuthServiceMock = new Mock<ICookieAuthService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<RefreshTokenCommandHandler>>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        // Setup current user with default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "NOIRSecretKeyForJWTAuthenticationMustBeAtLeast32Characters!",
            Issuer = "NOIR.API",
            Audience = "NOIR.Client",
            ExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        });

        _handler = new RefreshTokenCommandHandler(
            _userIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _deviceFingerprintServiceMock.Object,
            _cookieAuthServiceMock.Object,
            _localizationServiceMock.Object,
            _currentUserMock.Object,
            jwtSettings,
            _loggerMock.Object);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private ClaimsPrincipal CreateTestPrincipal(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private void SetupSuccessfulTokenRotation(UserIdentityDto user)
    {
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var newRefreshToken = RefreshToken.Create(GenerateTestToken(), user.Id, 7, user.TenantId);
        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.TenantId)) // Use user.TenantId from database, not _currentUser.TenantId
            .Returns("new-access-token");

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidTokens_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        SetupSuccessfulTokenRotation(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.AccessToken.Should().Be("new-access-token");
    }

    [Fact]
    public async Task Handle_ValidTokens_ShouldReturnNewRefreshToken()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        SetupSuccessfulTokenRotation(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidTokens_ShouldCallTokenRotation()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        SetupSuccessfulTokenRotation(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RotateTokenAsync(
                "valid-refresh-token",
                "127.0.0.1",
                "test-fingerprint",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTokens_WithTenant_ShouldGenerateTokenWithUserTenantId()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        SetupSuccessfulTokenRotation(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Handler should use user.TenantId from database, not _currentUser.TenantId
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user.Id, user.Email, user.TenantId),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios - Invalid Access Token

    [Fact]
    public async Task Handle_InvalidAccessToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-access-token", "valid-refresh-token");

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns((ClaimsPrincipal?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("accessTokenInvalid");
    }

    [Fact]
    public async Task Handle_AccessTokenWithoutUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand("access-token-without-userid", "valid-refresh-token");

        // Principal without NameIdentifier claim
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("accessTokenInvalid");
    }

    [Fact]
    public async Task Handle_InvalidAccessToken_ShouldNotCallUserIdentityService()
    {
        // Arrange
        var command = new RefreshTokenCommand("invalid-access-token", "valid-refresh-token");

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns((ClaimsPrincipal?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        var principal = CreateTestPrincipal("non-existent-user");

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("non-existent-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("user.notFound");
    }

    #endregion

    #region Failure Scenarios - Disabled User

    [Fact]
    public async Task Handle_DisabledUser_ShouldReturnForbidden()
    {
        // Arrange
        var user = CreateTestUserDto(isActive: false);
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("accountDisabled");
    }

    [Fact]
    public async Task Handle_DisabledUser_ShouldNotAttemptTokenRotation()
    {
        // Arrange
        var user = CreateTestUserDto(isActive: false);
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RotateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - Token Rotation Failure

    [Fact]
    public async Task Handle_TokenRotationFails_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "expired-refresh-token");
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("refreshTokenInvalid");
    }

    [Fact]
    public async Task Handle_TokenRotationFails_ShouldLogWarning()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "reused-refresh-token");
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token rotation failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TokenRotationFails_ShouldNotGenerateNewAccessToken()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "invalid-refresh-token");
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    #endregion

    #region Device Fingerprint Tests

    [Fact]
    public async Task Handle_ShouldCollectDeviceInfo()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token");
        SetupSuccessfulTokenRotation(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _deviceFingerprintServiceMock.Verify(x => x.GetClientIpAddress(), Times.Once);
        _deviceFingerprintServiceMock.Verify(x => x.GenerateFingerprint(), Times.Once);
    }

    #endregion

    #region Cookie Auth Tests

    [Fact]
    public async Task Handle_UseCookiesTrue_ShouldSetAuthCookies()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token", UseCookies: true);
        SetupSuccessfulTokenRotation(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(
            x => x.SetAuthCookies(
                "new-access-token",
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UseCookiesFalse_ShouldNotSetCookies()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token", UseCookies: false);
        SetupSuccessfulTokenRotation(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cookieAuthServiceMock.Verify(
            x => x.SetAuthCookies(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NullRefreshToken_WithCookies_ShouldGetFromCookie()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", RefreshToken: null, UseCookies: true);
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _cookieAuthServiceMock
            .Setup(x => x.GetRefreshTokenFromCookie())
            .Returns("cookie-refresh-token");

        var newRefreshToken = RefreshToken.Create(GenerateTestToken(), user.Id, 7);
        _refreshTokenServiceMock
            .Setup(x => x.RotateTokenAsync(
                "cookie-refresh-token",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRefreshToken);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, TestTenantId))
            .Returns("new-access-token");

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(x => x.GetRefreshTokenFromCookie(), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRefreshToken_NoCookie_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new RefreshTokenCommand("valid-access-token", RefreshToken: null, UseCookies: true);
        var principal = CreateTestPrincipal(user.Id);

        _tokenServiceMock
            .Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>()))
            .Returns(principal);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _cookieAuthServiceMock
            .Setup(x => x.GetRefreshTokenFromCookie())
            .Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("refreshTokenRequired");
    }

    #endregion
}
