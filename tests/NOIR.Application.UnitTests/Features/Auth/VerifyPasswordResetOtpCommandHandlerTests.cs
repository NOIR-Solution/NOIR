namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for VerifyPasswordResetOtpCommandHandler.
/// Tests password reset OTP verification scenarios.
/// </summary>
public class VerifyPasswordResetOtpCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly VerifyPasswordResetOtpCommandHandler _handler;

    public VerifyPasswordResetOtpCommandHandlerTests()
    {
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _handler = new VerifyPasswordResetOtpCommandHandler(_passwordResetServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidOtp_ShouldReturnResetToken()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "123456";
        const string resetToken = "reset-token-abc";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetVerifyResult(resetToken, DateTimeOffset.UtcNow.AddMinutes(15))));

        var command = new VerifyPasswordResetOtpCommand(sessionToken, otp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResetToken.Should().Be(resetToken);
    }

    [Fact]
    public async Task Handle_WithValidOtp_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "654321";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetVerifyResult("reset-token", DateTimeOffset.UtcNow.AddMinutes(15))));

        var command = new VerifyPasswordResetOtpCommand(sessionToken, otp);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithInvalidOtp_ShouldReturnFailure()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string invalidOtp = "000000";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, invalidOtp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetVerifyResult>(
                Error.Validation("Otp", "Invalid OTP code")));

        var command = new VerifyPasswordResetOtpCommand(sessionToken, invalidOtp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Invalid OTP");
    }

    [Fact]
    public async Task Handle_WithExpiredSession_ShouldReturnFailure()
    {
        // Arrange
        const string expiredSessionToken = "expired-session-token";
        const string otp = "123456";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(expiredSessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetVerifyResult>(
                Error.NotFound("Session expired or not found")));

        var command = new VerifyPasswordResetOtpCommand(expiredSessionToken, otp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WithTooManyAttempts_ShouldReturnFailure()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "wrong-otp";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetVerifyResult>(
                Error.TooManyRequests("Too many verification attempts")));

        var command = new VerifyPasswordResetOtpCommand(sessionToken, otp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.TooManyRequests);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "123456";

        _passwordResetServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetVerifyResult("reset-token", DateTimeOffset.UtcNow.AddMinutes(15))));

        var command = new VerifyPasswordResetOtpCommand(sessionToken, otp);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.VerifyOtpAsync(sessionToken, otp, token),
            Times.Once);
    }

    #endregion
}
