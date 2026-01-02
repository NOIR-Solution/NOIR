namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RegisterCommandHandler.
/// Tests user registration scenarios with mocked dependencies.
/// </summary>
public class RegisterCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICookieAuthService> _cookieAuthServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _cookieAuthServiceMock = new Mock<ICookieAuthService>();

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "NOIRSecretKeyForJWTAuthenticationMustBeAtLeast32Characters!",
            Issuer = "NOIR.API",
            Audience = "NOIR.Client",
            ExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        });

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _cookieAuthServiceMock.Object,
            jwtSettings);
    }

    private void SetupSuccessfulRegistration()
    {
        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create("user-id", 7);
        _refreshTokenServiceMock
            .Setup(x => x.CreateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", "John", "Doe");
        SetupSuccessfulRegistration();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("new@example.com");
        result.Value.AccessToken.Should().Be("test-access-token");
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateUser()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", "John", "Doe");
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u =>
                    u.Email == "new@example.com" &&
                    u.FirstName == "John" &&
                    u.LastName == "Doe" &&
                    u.IsActive == true),
                "password123"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignUserRole()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateRefreshToken()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.CreateTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MinimalCommand_ShouldSucceed()
    {
        // Arrange - Only required fields
        var command = new RegisterCommand("new@example.com", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnValidationError()
    {
        // Arrange
        var command = new RegisterCommand("existing@example.com", "password123", null, null);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "DuplicateEmail", Description = "Email 'existing@example.com' is already taken." }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("already taken");
    }

    [Fact]
    public async Task Handle_WeakPassword_ShouldReturnValidationError()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "weak", null, null);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Passwords must have at least one digit." }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("6 characters");
    }

    [Fact]
    public async Task Handle_MultipleIdentityErrors_ShouldCombineMessages()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "weak", null, null);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "Error1", Description = "First error." },
                new IdentityError { Code = "Error2", Description = "Second error." }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("First error");
        result.Error.Message.Should().Contain("Second error");
    }

    [Fact]
    public async Task Handle_FailedRegistration_ShouldNotAssignRole()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "Error", Description = "Failed" }));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Email Normalization Tests

    [Fact]
    public async Task Handle_ShouldNormalizeEmail()
    {
        // Arrange
        var command = new RegisterCommand("Test@Example.COM", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => u.NormalizedEmail == "TEST@EXAMPLE.COM"),
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region User Properties Tests

    [Fact]
    public async Task Handle_ShouldSetUserAsActive()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => u.IsActive == true),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetUsernameToEmail()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null);
        SetupSuccessfulRegistration();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => u.UserName == "new@example.com"),
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Cookie Auth Tests

    [Fact]
    public async Task Handle_UseCookiesTrue_ShouldSetAuthCookies()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", "John", "Doe", UseCookies: true);
        SetupSuccessfulRegistration();

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
        var command = new RegisterCommand("new@example.com", "password123", "John", "Doe", UseCookies: false);
        SetupSuccessfulRegistration();

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
        var command = new RegisterCommand("new@example.com", "password123", null, null); // Default UseCookies = false
        SetupSuccessfulRegistration();

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
    public async Task Handle_FailedRegistration_ShouldNotSetCookies()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "password123", null, null, UseCookies: true);

        _userManagerMock
            .Setup(x => x.NormalizeEmail(It.IsAny<string>()))
            .Returns<string>(e => e.ToUpperInvariant());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "Error", Description = "Registration failed" }));

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
