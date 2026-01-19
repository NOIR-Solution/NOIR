namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for VerifyEmailChangeCommandHandler.
/// Tests email change OTP verification scenarios.
/// </summary>
public class VerifyEmailChangeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IEmailChangeService> _emailChangeServiceMock;
    private readonly VerifyEmailChangeCommandHandler _handler;

    public VerifyEmailChangeCommandHandlerTests()
    {
        _emailChangeServiceMock = new Mock<IEmailChangeService>();
        _handler = new VerifyEmailChangeCommandHandler(_emailChangeServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidOtp_ShouldReturnSuccess()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "123456";
        const string newEmail = "newemail@example.com";

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeVerifyResult(newEmail, "Email changed successfully")));

        var command = new VerifyEmailChangeCommand(sessionToken, otp);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewEmail.Should().Be(newEmail);
    }

    [Fact]
    public async Task Handle_WithValidOtp_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        const string otp = "654321";

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeVerifyResult("newemail@example.com", "Email changed successfully")));

        var command = new VerifyEmailChangeCommand(sessionToken, otp);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailChangeServiceMock.Verify(
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

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, invalidOtp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeVerifyResult>(
                Error.Validation("Otp", "Invalid OTP code")));

        var command = new VerifyEmailChangeCommand(sessionToken, invalidOtp);

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

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(expiredSessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeVerifyResult>(
                Error.NotFound("Session expired or not found")));

        var command = new VerifyEmailChangeCommand(expiredSessionToken, otp);

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

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeVerifyResult>(
                Error.TooManyRequests("Too many verification attempts")));

        var command = new VerifyEmailChangeCommand(sessionToken, otp);

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

        _emailChangeServiceMock
            .Setup(x => x.VerifyOtpAsync(sessionToken, otp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeVerifyResult("newemail@example.com", "Email changed successfully")));

        var command = new VerifyEmailChangeCommand(sessionToken, otp);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _emailChangeServiceMock.Verify(
            x => x.VerifyOtpAsync(sessionToken, otp, token),
            Times.Once);
    }

    #endregion
}
