namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Tests all authentication scenarios with mocked dependencies.
/// </summary>
public class LoginCommandHandlerTests
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
    private readonly LoginCommandHandler _handler;
    private const string TestTenantId = "tenant-abc";

    public LoginCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _deviceFingerprintServiceMock = new Mock<IDeviceFingerprintService>();
        _cookieAuthServiceMock = new Mock<ICookieAuthService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _currentUserMock = new Mock<ICurrentUser>();

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

        _handler = new LoginCommandHandler(
            _userIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _deviceFingerprintServiceMock.Object,
            _cookieAuthServiceMock.Object,
            _localizationServiceMock.Object,
            _currentUserMock.Object,
            jwtSettings);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private void SetupSuccessfulLogin(UserIdentityDto user)
    {
        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: true, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, TestTenantId))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create(GenerateTestToken(), user.Id, 7, TestTenantId);
        _refreshTokenServiceMock
            .Setup(x => x.CreateTokenAsync(
                user.Id,
                TestTenantId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        _deviceFingerprintServiceMock
            .Setup(x => x.GetClientIpAddress())
            .Returns("127.0.0.1");

        _deviceFingerprintServiceMock
            .Setup(x => x.GenerateFingerprint())
            .Returns("test-fingerprint");

        _deviceFingerprintServiceMock
            .Setup(x => x.GetUserAgent())
            .Returns("Test User Agent");

        _deviceFingerprintServiceMock
            .Setup(x => x.GetDeviceName())
            .Returns("Test Device");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.AccessToken.Should().Be("test-access-token");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnRefreshToken()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldCallTokenServices()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user.Id, user.Email, TestTenantId),
            Times.Once);

        _refreshTokenServiceMock.Verify(
            x => x.CreateTokenAsync(
                user.Id,
                TestTenantId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCredentials_WithTenant_ShouldIncludeTenantId()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // TenantId comes from ICurrentUser context, not from user entity
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user.Id, user.Email, TestTenantId),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("invalidCredentials");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldNotCallPasswordCheck()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.CheckPasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - Disabled User

    [Fact]
    public async Task Handle_DisabledUser_ShouldReturnForbidden()
    {
        // Arrange
        var user = CreateTestUserDto(isActive: false);
        var command = new LoginCommand("test@example.com", "validPassword123");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("accountDisabled");
    }

    [Fact]
    public async Task Handle_DisabledUser_ShouldNotCheckPassword()
    {
        // Arrange
        var user = CreateTestUserDto(isActive: false);
        var command = new LoginCommand("test@example.com", "validPassword123");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.CheckPasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - Wrong Password

    [Fact]
    public async Task Handle_WrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "wrongPassword");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: false, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("invalidCredentials");
    }

    #endregion

    #region Failure Scenarios - Account Locked

    [Fact]
    public async Task Handle_LockedOutUser_ShouldReturnForbidden()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "password");

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: false, IsLockedOut: true, IsNotAllowed: false, RequiresTwoFactor: false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("accountLockedOut");
    }

    #endregion

    #region Device Fingerprint Tests

    [Fact]
    public async Task Handle_ShouldCollectDeviceInfo()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _deviceFingerprintServiceMock.Verify(x => x.GetClientIpAddress(), Times.Once);
        _deviceFingerprintServiceMock.Verify(x => x.GenerateFingerprint(), Times.Once);
        _deviceFingerprintServiceMock.Verify(x => x.GetUserAgent(), Times.Once);
        _deviceFingerprintServiceMock.Verify(x => x.GetDeviceName(), Times.Once);
    }

    #endregion

    #region Cookie Auth Tests

    [Fact]
    public async Task Handle_UseCookiesTrue_ShouldSetAuthCookies()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123", UseCookies: true);
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _cookieAuthServiceMock.Verify(
            x => x.SetAuthCookies(
                "test-access-token",
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
        var command = new LoginCommand("test@example.com", "validPassword123", UseCookies: false);
        SetupSuccessfulLogin(user);

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
    public async Task Handle_UseCookiesDefault_ShouldNotSetCookies()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123"); // Default UseCookies = false
        SetupSuccessfulLogin(user);

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
    public async Task Handle_FailedLogin_ShouldNotSetCookies()
    {
        // Arrange
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "wrongPassword", UseCookies: true);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: false, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        _cookieAuthServiceMock.Verify(
            x => x.SetAuthCookies(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>()),
            Times.Never);
    }

    #endregion
}
