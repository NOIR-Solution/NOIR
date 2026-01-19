namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for ResendPasswordResetOtpCommandHandler.
/// Tests resending password reset OTP scenarios.
/// </summary>
public class ResendPasswordResetOtpCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly ResendPasswordResetOtpCommandHandler _handler;

    public ResendPasswordResetOtpCommandHandlerTests()
    {
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _handler = new ResendPasswordResetOtpCommandHandler(_passwordResetServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidSession_ShouldReturnSuccess()
    {
        // Arrange
        const string sessionToken = "session-token-123";
        var nextResendTime = DateTimeOffset.UtcNow.AddMinutes(1);

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetResendResult(
                true,
                nextResendTime,
                2)));

        var command = new ResendPasswordResetOtpCommand(sessionToken);

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

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetResendResult(
                true,
                DateTimeOffset.UtcNow.AddMinutes(1),
                3)));

        var command = new ResendPasswordResetOtpCommand(sessionToken);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordResetServiceMock.Verify(
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

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(expiredSessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetResendResult>(
                Error.NotFound("Session expired or not found")));

        var command = new ResendPasswordResetOtpCommand(expiredSessionToken);

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

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetResendResult>(
                Error.TooManyRequests("Please wait before requesting another OTP")));

        var command = new ResendPasswordResetOtpCommand(sessionToken);

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

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetResendResult>(
                Error.Validation("session", "No resend attempts remaining")));

        var command = new ResendPasswordResetOtpCommand(sessionToken);

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

        _passwordResetServiceMock
            .Setup(x => x.ResendOtpAsync(sessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetResendResult(
                true,
                DateTimeOffset.UtcNow.AddMinutes(1),
                3)));

        var command = new ResendPasswordResetOtpCommand(sessionToken);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.ResendOtpAsync(sessionToken, token),
            Times.Once);
    }

    #endregion
}
