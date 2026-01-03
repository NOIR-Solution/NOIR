namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Tests all authentication scenarios with mocked dependencies.
/// </summary>
public class LoginCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IDeviceFingerprintService> _deviceFingerprintServiceMock;
    private readonly Mock<ICookieAuthService> _cookieAuthServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        // UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // SignInManager mock
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _deviceFingerprintServiceMock = new Mock<IDeviceFingerprintService>();
        _cookieAuthServiceMock = new Mock<ICookieAuthService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "NOIRSecretKeyForJWTAuthenticationMustBeAtLeast32Characters!",
            Issuer = "NOIR.API",
            Audience = "NOIR.Client",
            ExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        });

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _deviceFingerprintServiceMock.Object,
            _cookieAuthServiceMock.Object,
            _localizationServiceMock.Object,
            jwtSettings);
    }

    private ApplicationUser CreateTestUser(
        string id = "user-123",
        string email = "test@example.com",
        bool isActive = true,
        string? tenantId = null)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            IsActive = isActive,
            TenantId = tenantId
        };
    }

    private void SetupSuccessfulLogin(ApplicationUser user)
    {
        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Success);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email!, user.TenantId))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create(user.Id, 7, user.TenantId);
        _refreshTokenServiceMock
            .Setup(x => x.CreateTokenAsync(
                user.Id,
                user.TenantId,
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
        var user = CreateTestUser();
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
        var user = CreateTestUser();
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
        var user = CreateTestUser();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user.Id, user.Email!, user.TenantId),
            Times.Once);

        _refreshTokenServiceMock.Verify(
            x => x.CreateTokenAsync(
                user.Id,
                user.TenantId,
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
        var user = CreateTestUser(tenantId: "tenant-abc");
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _tokenServiceMock.Verify(
            x => x.GenerateAccessToken(user.Id, user.Email!, "tenant-abc"),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("invalidCredentials");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldNotCallSignInManager()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _signInManagerMock.Verify(
            x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - Disabled User

    [Fact]
    public async Task Handle_DisabledUser_ShouldReturnForbidden()
    {
        // Arrange
        var user = CreateTestUser(isActive: false);
        var command = new LoginCommand("test@example.com", "validPassword123");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
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
        var user = CreateTestUser(isActive: false);
        var command = new LoginCommand("test@example.com", "validPassword123");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _signInManagerMock.Verify(
            x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - Wrong Password

    [Fact]
    public async Task Handle_WrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new LoginCommand("test@example.com", "wrongPassword");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Failed);

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
        var user = CreateTestUser();
        var command = new LoginCommand("test@example.com", "password");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("accountLockedOut");
    }

    #endregion

    #region Email Normalization Tests

    [Fact]
    public async Task Handle_ShouldNormalizeEmail()
    {
        // Arrange
        var command = new LoginCommand("Test@Example.COM", "password");

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.NormalizeEmail("Test@Example.COM"),
            Times.Once);
    }

    #endregion

    #region Device Fingerprint Tests

    [Fact]
    public async Task Handle_ShouldCollectDeviceInfo()
    {
        // Arrange
        var user = CreateTestUser();
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
        var user = CreateTestUser();
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
        var user = CreateTestUser();
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
        var user = CreateTestUser();
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
        var user = CreateTestUser();
        var command = new LoginCommand("test@example.com", "wrongPassword", UseCookies: true);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Failed);

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
