namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RegisterCommandHandler.
/// Tests user registration scenarios with mocked dependencies.
/// </summary>
public class RegisterCommandHandlerTests
{
    private static string GenerateTestToken() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ICookieAuthService> _cookieAuthServiceMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
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
            _userIdentityServiceMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _cookieAuthServiceMock.Object,
            jwtSettings);
    }

    private void SetupSuccessfulRegistration(string userId = "new-user-id")
    {
        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.AddToRolesAsync(userId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        var userDto = new UserIdentityDto(
            Id: userId,
            Email: "new@example.com",
            FirstName: "John",
            LastName: "Doe",
            DisplayName: null,
            FullName: "John Doe",
            TenantId: null,
            IsActive: true,
            IsDeleted: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns("test-access-token");

        var refreshToken = RefreshToken.Create(GenerateTestToken(), userId, 7);
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
        _userIdentityServiceMock.Verify(
            x => x.CreateUserAsync(
                It.Is<CreateUserDto>(dto =>
                    dto.Email == "new@example.com" &&
                    dto.FirstName == "John" &&
                    dto.LastName == "Doe"),
                "password123",
                It.IsAny<CancellationToken>()),
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
        _userIdentityServiceMock.Verify(
            x => x.AddToRolesAsync(
                It.IsAny<string>(),
                It.Is<IEnumerable<string>>(roles => roles.Contains("User")),
                It.IsAny<CancellationToken>()),
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

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Email 'existing@example.com' is already taken."));

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

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure(
                "Passwords must be at least 6 characters.",
                "Passwords must have at least one digit."));

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

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("First error.", "Second error."));

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

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Failed"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.AddToRolesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Registration failed"));

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
