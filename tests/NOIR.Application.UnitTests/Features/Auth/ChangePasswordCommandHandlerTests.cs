namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for ChangePasswordCommandHandler.
/// Tests password change scenarios with mocked dependencies.
/// </summary>
public class ChangePasswordCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new ChangePasswordCommandHandler(
            _currentUserMock.Object,
            _userIdentityServiceMock.Object,
            _refreshTokenServiceMock.Object,
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

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        const string currentPassword = "OldPassword123!";
        const string newPassword = "NewPassword456!";

        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new ChangePasswordCommand(currentPassword, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.ChangePasswordAsync(userId, currentPassword, newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldRevokeAllSessions()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(
                userId,
                It.IsAny<string?>(),
                It.Is<string?>(r => r != null && r.Contains("Password changed")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Authentication Failure Scenarios

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
        _userIdentityServiceMock.Verify(
            x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

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
        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Password Validation Failure Scenarios

    [Fact]
    public async Task Handle_WithIncorrectCurrentPassword_ShouldReturnInvalidPassword()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Incorrect password."));

        var command = new ChangePasswordCommand("WrongPassword!", "NewPassword456!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.InvalidPassword);
    }

    [Fact]
    public async Task Handle_WithWeakNewPassword_ShouldReturnValidationError()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Password must be at least 8 characters.", "Password must have at least one digit."));

        var command = new ChangePasswordCommand("OldPassword123!", "weak");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WhenChangePasswordFails_ShouldNotRevokeSessions()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Password change failed."));

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Sessions should NOT be revoked if password change fails
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullErrorsFromIdentity_ShouldHandleGracefully()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(false, null, null));

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";
        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new ChangePasswordCommand("OldPassword123!", "NewPassword456!");
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.ChangePasswordAsync(userId, It.IsAny<string>(), It.IsAny<string>(), token),
            Times.Once);
        _refreshTokenServiceMock.Verify(
            x => x.RevokeAllUserTokensAsync(userId, It.IsAny<string?>(), It.IsAny<string?>(), token),
            Times.Once);
    }

    #endregion
}
