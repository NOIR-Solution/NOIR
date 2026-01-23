using NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;

namespace NOIR.Application.UnitTests.Features.TenantSettings;

/// <summary>
/// Unit tests for TestTenantSmtpConnectionCommandHandler.
/// Tests SMTP connection testing via the tenant's configured SMTP settings.
/// </summary>
public class TestTenantSmtpConnectionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<ISmtpTestService> _smtpTestServiceMock;
    private readonly TestTenantSmtpConnectionCommandHandler _handler;

    public TestTenantSmtpConnectionCommandHandlerTests()
    {
        _smtpTestServiceMock = new Mock<ISmtpTestService>();
        _handler = new TestTenantSmtpConnectionCommandHandler(_smtpTestServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenSmtpTestSucceeds_ShouldReturnSuccess()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPassRecipientEmailToService()
    {
        // Arrange
        const string recipientEmail = "admin@company.com";
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _smtpTestServiceMock.Verify(
            x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenSmtpTestFails_ShouldReturnFailure()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(
                Error.Internal("SMTP connection failed: Connection refused", ErrorCodes.System.InternalError)));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("SMTP connection failed");
    }

    [Fact]
    public async Task Handle_WhenSmtpTimeouts_ShouldReturnFailure()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(
                Error.Internal("Connection timed out", ErrorCodes.System.InternalError)));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("timed out");
    }

    [Fact]
    public async Task Handle_WhenAuthenticationFails_ShouldReturnFailure()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(
                Error.Internal("Authentication failed: Invalid credentials", ErrorCodes.System.InternalError)));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Authentication failed");
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, token))
            .ReturnsAsync(Result.Success(true));

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _smtpTestServiceMock.Verify(
            x => x.SendTestEmailAsync(recipientEmail, token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        const string recipientEmail = "test@example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var command = new TestTenantSmtpConnectionCommand(recipientEmail);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cts.Token));
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("admin@company.org")]
    [InlineData("test.user+tag@domain.co.uk")]
    public async Task Handle_WithVariousEmailFormats_ShouldPassToService(string email)
    {
        // Arrange
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var command = new TestTenantSmtpConnectionCommand(email);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _smtpTestServiceMock.Verify(
            x => x.SendTestEmailAsync(email, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
