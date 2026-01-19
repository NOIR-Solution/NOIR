namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for ResetPasswordCommandHandler.
/// Tests final password reset scenarios using the reset token.
/// </summary>
public class ResetPasswordCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new ResetPasswordCommandHandler(
            _passwordResetServiceMock.Object,
            _localizationServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidResetToken_ShouldReturnSuccess()
    {
        // Arrange
        const string resetToken = "reset-token-123";
        const string newPassword = "NewSecurePassword123!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(resetToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var command = new ResetPasswordCommand(resetToken, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Be("auth.passwordReset.success");
    }

    [Fact]
    public async Task Handle_WithValidResetToken_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const string resetToken = "reset-token-456";
        const string newPassword = "AnotherPassword456!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(resetToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var command = new ResetPasswordCommand(resetToken, newPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.ResetPasswordAsync(resetToken, newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithInvalidResetToken_ShouldReturnFailure()
    {
        // Arrange
        const string invalidToken = "invalid-token";
        const string newPassword = "NewPassword123!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(invalidToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                Error.NotFound("Invalid or expired reset token")));

        var command = new ResetPasswordCommand(invalidToken, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Invalid or expired reset token");
    }

    [Fact]
    public async Task Handle_WithExpiredResetToken_ShouldReturnFailure()
    {
        // Arrange
        const string expiredToken = "expired-token";
        const string newPassword = "NewPassword123!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(expiredToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                Error.NotFound("Reset token has expired")));

        var command = new ResetPasswordCommand(expiredToken, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WithWeakPassword_ShouldReturnValidationError()
    {
        // Arrange
        const string resetToken = "reset-token-123";
        const string weakPassword = "weak";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(resetToken, weakPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                Error.Validation("Password", "Password does not meet requirements")));

        var command = new ResetPasswordCommand(resetToken, weakPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenAlreadyUsed_ShouldReturnFailure()
    {
        // Arrange
        const string usedToken = "already-used-token";
        const string newPassword = "NewPassword123!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(usedToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                Error.Conflict("Reset token has already been used")));

        var command = new ResetPasswordCommand(usedToken, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        const string resetToken = "reset-token-123";
        const string newPassword = "NewPassword123!";

        _passwordResetServiceMock
            .Setup(x => x.ResetPasswordAsync(resetToken, newPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var command = new ResetPasswordCommand(resetToken, newPassword);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.ResetPasswordAsync(resetToken, newPassword, token),
            Times.Once);
    }

    #endregion
}
