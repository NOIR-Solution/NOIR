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
        bool isActive = true,
        string? tenantId = TestTenantId)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: tenantId,
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

    private void SetupSuccessfulLogin(UserIdentityDto user)
    {
        // Setup FindTenantsByEmailAsync to return the user's tenant info
        var tenantInfo = new UserTenantInfo(
            user.Id,
            user.TenantId,
            "Test Tenant",
            "test-tenant");

        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: true, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.TenantId))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create(GenerateTestToken(), user.Id, 7, user.TenantId);
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
        var user = CreateTestUserDto();
        var command = new LoginCommand("test@example.com", "validPassword123");
        SetupSuccessfulLogin(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Success.Should().BeTrue();
        result.Value.Auth.Should().NotBeNull();
        result.Value.Auth!.UserId.Should().Be(user.Id);
        result.Value.Auth.Email.Should().Be(user.Email);
        result.Value.Auth.AccessToken.Should().Be("test-access-token");
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
        result.Value.Success.Should().BeTrue();
        result.Value.Auth.Should().NotBeNull();
        result.Value.Auth!.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.Auth.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
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

        // No tenants have this email
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo>());

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

        // No tenants have this email
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo>());

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
        // Provide TenantId to trigger direct tenant authentication path
        var command = new LoginCommand("test@example.com", "validPassword123", TenantId: TestTenantId);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), TestTenantId, It.IsAny<CancellationToken>()))
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
        // Provide TenantId to trigger direct tenant authentication path
        var command = new LoginCommand("test@example.com", "validPassword123", TenantId: TestTenantId);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), TestTenantId, It.IsAny<CancellationToken>()))
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

        var tenantInfo = new UserTenantInfo(user.Id, user.TenantId, "Test Tenant", "test-tenant");
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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

        var tenantInfo = new UserTenantInfo(user.Id, user.TenantId, "Test Tenant", "test-tenant");
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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

        var tenantInfo = new UserTenantInfo(user.Id, user.TenantId, "Test Tenant", "test-tenant");
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserTenantInfo> { tenantInfo });

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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

    #region Multi-Tenant Scenarios

    [Fact]
    public async Task Handle_MultipleTenantsMatched_ShouldReturnTenantSelection()
    {
        // Arrange
        var user1 = CreateTestUserDto(id: "user-1", tenantId: "tenant-a");
        var user2 = CreateTestUserDto(id: "user-2", tenantId: "tenant-b");
        var command = new LoginCommand("test@example.com", "validPassword123");

        // Setup two tenants with same email
        var tenantInfos = new List<UserTenantInfo>
        {
            new("user-1", "tenant-a", "Company A", "company-a"),
            new("user-2", "tenant-b", "Company B", "company-b")
        };
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantInfos);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), "tenant-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);
        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), "tenant-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        // Password matches for both
        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: true, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.RequiresTenantSelection.Should().BeTrue();
        result.Value.AvailableTenants.Should().HaveCount(2);
        result.Value.AvailableTenants![0].Name.Should().Be("Company A");
        result.Value.AvailableTenants![1].Name.Should().Be("Company B");
        result.Value.Auth.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TenantIdProvided_ShouldAuthenticateDirectlyToTenant()
    {
        // Arrange
        var user = CreateTestUserDto();
        // TenantId provided - used after user selects from tenant dialog
        var command = new LoginCommand("test@example.com", "validPassword123", TenantId: TestTenantId);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync(user.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: true, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.TenantId))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create(GenerateTestToken(), user.Id, 7, user.TenantId);
        _refreshTokenServiceMock
            .Setup(x => x.CreateTokenAsync(user.Id, user.TenantId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        _deviceFingerprintServiceMock.Setup(x => x.GetClientIpAddress()).Returns("127.0.0.1");
        _deviceFingerprintServiceMock.Setup(x => x.GenerateFingerprint()).Returns("test-fingerprint");
        _deviceFingerprintServiceMock.Setup(x => x.GetUserAgent()).Returns("Test User Agent");
        _deviceFingerprintServiceMock.Setup(x => x.GetDeviceName()).Returns("Test Device");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Direct login, no tenant selection
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.RequiresTenantSelection.Should().BeFalse();
        result.Value.Auth.Should().NotBeNull();
        result.Value.AvailableTenants.Should().BeNull();

        // Verify FindTenantsByEmailAsync was NOT called (bypassed when TenantId provided)
        _userIdentityServiceMock.Verify(
            x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleTenantsButOnlyOnePasswordMatches_ShouldLoginDirectly()
    {
        // Arrange - User exists in 2 tenants but password only matches in one
        var user1 = CreateTestUserDto(id: "user-1", tenantId: "tenant-a");
        var user2 = CreateTestUserDto(id: "user-2", tenantId: "tenant-b");
        var command = new LoginCommand("test@example.com", "validPassword123");

        var tenantInfos = new List<UserTenantInfo>
        {
            new("user-1", "tenant-a", "Company A", "company-a"),
            new("user-2", "tenant-b", "Company B", "company-b")
        };
        _userIdentityServiceMock
            .Setup(x => x.FindTenantsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantInfos);

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), "tenant-a", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user1);
        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), "tenant-b", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user2);

        // Password matches in tenant-a but not tenant-b
        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync("user-1", command.Password, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: true, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));
        _userIdentityServiceMock
            .Setup(x => x.CheckPasswordSignInAsync("user-2", command.Password, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordSignInResult(Succeeded: false, IsLockedOut: false, IsNotAllowed: false, RequiresTwoFactor: false));

        // Setup token services for tenant-a
        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user1.Id, user1.Email, user1.TenantId))
            .Returns("test-access-token");
        var refreshToken = RefreshToken.Create(GenerateTestToken(), user1.Id, 7, user1.TenantId);
        _refreshTokenServiceMock
            .Setup(x => x.CreateTokenAsync(user1.Id, user1.TenantId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        _deviceFingerprintServiceMock.Setup(x => x.GetClientIpAddress()).Returns("127.0.0.1");
        _deviceFingerprintServiceMock.Setup(x => x.GenerateFingerprint()).Returns("test-fingerprint");
        _deviceFingerprintServiceMock.Setup(x => x.GetUserAgent()).Returns("Test User Agent");
        _deviceFingerprintServiceMock.Setup(x => x.GetDeviceName()).Returns("Test Device");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should complete login directly, not prompt for tenant selection
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.RequiresTenantSelection.Should().BeFalse();
        result.Value.Auth.Should().NotBeNull();
        result.Value.Auth!.UserId.Should().Be("user-1");
    }

    #endregion
}
