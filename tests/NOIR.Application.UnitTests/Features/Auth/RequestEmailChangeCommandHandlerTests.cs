namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for RequestEmailChangeCommandHandler.
/// Tests email change request initiation scenarios.
/// </summary>
public class RequestEmailChangeCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IEmailChangeService> _emailChangeServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly RequestEmailChangeCommandHandler _handler;

    public RequestEmailChangeCommandHandlerTests()
    {
        _emailChangeServiceMock = new Mock<IEmailChangeService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new RequestEmailChangeCommandHandler(
            _emailChangeServiceMock.Object,
            _localizationServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidUser_ShouldCallEmailChangeService()
    {
        // Arrange
        const string userId = "user-123";
        const string newEmail = "newemail@example.com";
        const string sessionToken = "session-token-123";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailChangeServiceMock
            .Setup(x => x.RequestEmailChangeAsync(
                userId,
                newEmail,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeRequestResult(
                sessionToken,
                "new***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestEmailChangeCommand(newEmail) { UserId = userId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionToken.Should().Be(sessionToken);
        _emailChangeServiceMock.Verify(
            x => x.RequestEmailChangeAsync(userId, newEmail, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithTenantAndIpAddress_ShouldPassThemToService()
    {
        // Arrange
        const string userId = "user-123";
        const string newEmail = "newemail@example.com";
        const string tenantId = "tenant-123";
        const string ipAddress = "192.168.1.1";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailChangeServiceMock
            .Setup(x => x.RequestEmailChangeAsync(
                userId,
                newEmail,
                tenantId,
                ipAddress,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeRequestResult(
                "session-token",
                "new***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestEmailChangeCommand(newEmail)
        {
            UserId = userId,
            TenantId = tenantId,
            IpAddress = ipAddress
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailChangeServiceMock.Verify(
            x => x.RequestEmailChangeAsync(userId, newEmail, tenantId, ipAddress, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Authentication Failure Scenarios

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new RequestEmailChangeCommand("newemail@example.com") { UserId = null };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
        _emailChangeServiceMock.Verify(
            x => x.RequestEmailChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new RequestEmailChangeCommand("newemail@example.com") { UserId = string.Empty };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    #endregion

    #region Rate Limiting Scenarios

    [Fact]
    public async Task Handle_WhenRateLimited_ShouldReturnTooManyRequests()
    {
        // Arrange
        const string userId = "user-123";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RequestEmailChangeCommand("newemail@example.com") { UserId = userId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.TooManyRequests);
        result.Error.Type.Should().Be(ErrorType.TooManyRequests);
        _emailChangeServiceMock.Verify(
            x => x.RequestEmailChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotRateLimited_ShouldProceedWithRequest()
    {
        // Arrange
        const string userId = "user-123";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailChangeServiceMock
            .Setup(x => x.RequestEmailChangeAsync(userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeRequestResult(
                "session-token",
                "new***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestEmailChangeCommand("newemail@example.com") { UserId = userId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Service Failure Scenarios

    [Fact]
    public async Task Handle_WhenServiceFails_ShouldReturnFailure()
    {
        // Arrange
        const string userId = "user-123";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailChangeServiceMock
            .Setup(x => x.RequestEmailChangeAsync(userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<EmailChangeRequestResult>(
                Error.Conflict("Email already in use")));

        var command = new RequestEmailChangeCommand("existing@example.com") { UserId = userId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Email already in use");
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";

        _emailChangeServiceMock
            .Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailChangeServiceMock
            .Setup(x => x.RequestEmailChangeAsync(userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new EmailChangeRequestResult(
                "session-token",
                "new***@example.com",
                DateTimeOffset.UtcNow.AddMinutes(10),
                6)));

        var command = new RequestEmailChangeCommand("newemail@example.com") { UserId = userId };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _emailChangeServiceMock.Verify(
            x => x.IsRateLimitedAsync(userId, token),
            Times.Once);
        _emailChangeServiceMock.Verify(
            x => x.RequestEmailChangeAsync(userId, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), token),
            Times.Once);
    }

    #endregion
}
