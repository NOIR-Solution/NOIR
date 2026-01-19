namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for ResendEmailChangeOtpCommandHandler.
/// Tests resending email change OTP scenarios.
/// </summary>
public class ResendEmailChangeOtpCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IEmailChangeService> _emailChangeServiceMock;
    private readonly ResendEmailChangeOtpCommandHandler _handler;

    public ResendEmailChangeOtpCommandHandlerTests()
    {
        _emailChangeServiceMock = new Mock<IEmailChangeService>();
        _handler = new ResendEmailChangeOtpCommandHandler(_emailChangeServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidSession_ShouldReturnSuccess()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        var nextResendTime = DateTimeOffset.UtcNow.AddMinutes(1);

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeResendResult(
                true,
                nextResendTime,
                2)));

        var command = new ResendEmailChangeOtpCommand(sessionToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.NextResendAt.Should().Be(nextResendTime);
        result.Value.RemainingResends.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithValidSession_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        const string sessionToken = "session-token-456";

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeResendResult(
                true,
                DateTimeOffset.UtcNow.AddMinutes(1),
                3)));

        var command = new ResendEmailChangeOtpCommand(sessionToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailChangeServiceMock.Verify(
            x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithExpiredSession_ShouldReturnFailure()
    {
        // Arrange
        const string expiredSessionToken = "expired-session-token";

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(expiredSessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeResendResult>(
                Error.NotFound("Session expired or not found")));

        var command = new ResendEmailChangeOtpCommand(expiredSessionToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCooldownActive_ShouldReturnFailure()
    {
        // Arrange
        const string sessionToken = "session-token-123";

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeResendResult>(
                Error.TooManyRequests("Please wait before requesting another OTP")));

        var command = new ResendEmailChangeOtpCommand(sessionToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.TooManyRequests);
    }

    [Fact]
    public async Task Handle_WhenNoAttemptsRemaining_ShouldReturnFailure()
    {
        // Arrange
        const string sessionToken = "session-token-123";

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeResendResult>(
                Error.Validation("session", "No resend attempts remaining")));

        var command = new ResendEmailChangeOtpCommand(sessionToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        const string sessionToken = "session-token-123";

        _emailChangeServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeResendResult(
                true,
                DateTimeOffset.UtcNow.AddMinutes(1),
                3)));

        var command = new ResendEmailChangeOtpCommand(sessionToken);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _emailChangeServiceMock.Verify(
            x => x.ResendOtpAsync(sessionToken, token),
            Times.Once);
    }

    #endregion
}
