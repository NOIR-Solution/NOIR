namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RequestPasswordResetCommandHandler.
/// Tests password reset request initiation scenarios.
/// </summary>
public class RequestPasswordResetCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly RequestPasswordResetCommandHandler _handler;

    public RequestPasswordResetCommandHandlerTests()
    {
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new RequestPasswordResetCommandHandler(
            _passwordResetServiceMock.Object,
            _localizationServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        const string email = "user@example.com";
        const string sessionToken = "session-token-123";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordResetServiceMock
            .Setup(x => x.RequestPasswordResetAsync(
                email,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetRequestResult(
                sessionToken,
                "us***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionToken.Should().Be(sessionToken);
    }

    [Fact]
    public async Task Handle_WithTenantAndIpAddress_ShouldPassThemToService()
    {
        // Arrange
        const string email = "user@example.com";
        const string tenantId = "tenant-123";
        const string ipAddress = "192.168.1.1";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordResetServiceMock
            .Setup(x => x.RequestPasswordResetAsync(
                email,
                tenantId,
                ipAddress,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetRequestResult(
                "session-token",
                "us***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestPasswordResetCommand(email)
        {
            TenantId = tenantId,
            IpAddress = ipAddress
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.RequestPasswordResetAsync(email, tenantId, ipAddress, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Rate Limiting Scenarios

    [Fact]
    public async Task Handle_WhenRateLimited_ShouldReturnTooManyRequests()
    {
        // Arrange
        const string email = "user@example.com";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.TooManyRequests);
        result.Error.Type.Should().Be(ErrorType.TooManyRequests);
        _passwordResetServiceMock.Verify(
            x => x.RequestPasswordResetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotRateLimited_ShouldProceedWithRequest()
    {
        // Arrange
        const string email = "user@example.com";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordResetServiceMock
            .Setup(x => x.RequestPasswordResetAsync(email, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetRequestResult(
                "session-token",
                "us***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _passwordResetServiceMock.Verify(
            x => x.RequestPasswordResetAsync(email, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Failure Scenarios

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldReturnFailure()
    {
        // Arrange
        const string email = "nonexistent@example.com";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordResetServiceMock
            .Setup(x => x.RequestPasswordResetAsync(email, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PasswordResetRequestResult>(
                Error.NotFound("User not found")));

        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string email = "user@example.com";

        _passwordResetServiceMock
            .Setup(x => x.IsRateLimitedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordResetServiceMock
            .Setup(x => x.RequestPasswordResetAsync(email, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PasswordResetRequestResult(
                "session-token",
                "us***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestPasswordResetCommand(email);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _passwordResetServiceMock.Verify(
            x => x.IsRateLimitedAsync(email, token),
            Times.Once);
        _passwordResetServiceMock.Verify(
            x => x.RequestPasswordResetAsync(email, It.IsAny<string?>(), It.IsAny<string?>(), token),
            Times.Once);
    }

    #endregion
}
