using NOIR.Application.Features.PlatformSettings.Commands.TestSmtpConnection;

namespace NOIR.Application.UnitTests.Features.PlatformSettings;

/// <summary>
/// Unit tests for TestSmtpConnectionCommandHandler.
/// Tests SMTP connection testing via ISmtpTestService.
/// </summary>
public class TestSmtpConnectionCommandHandlerTests
{
    private readonly Mock<ISmtpTestService> _smtpTestServiceMock;
    private readonly TestSmtpConnectionCommandHandler _handler;

    public TestSmtpConnectionCommandHandlerTests()
    {
        _smtpTestServiceMock = new Mock<ISmtpTestService>();
        _handler = new TestSmtpConnectionCommandHandler(_smtpTestServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSmtpTestSucceeds_ShouldReturnSuccess()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand("test@example.com");
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSmtpTestFails_ShouldReturnFailure()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand("test@example.com");
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(Error.Failure("SMTP.ConnectionFailed", "Connection failed")));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SMTP.ConnectionFailed");
    }

    [Fact]
    public async Task Handle_ShouldPassRecipientEmailToService()
    {
        // Arrange
        var recipientEmail = "recipient@example.com";
        var command = new TestSmtpConnectionCommand(recipientEmail);
        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _smtpTestServiceMock.Verify(
            x => x.SendTestEmailAsync(recipientEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand("test@example.com");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _smtpTestServiceMock
            .Setup(x => x.SendTestEmailAsync(It.IsAny<string>(), token))
            .ReturnsAsync(Result.Success(true));

        // Act
        await _handler.Handle(command, token);

        // Assert
        _smtpTestServiceMock.Verify(
            x => x.SendTestEmailAsync(It.IsAny<string>(), token),
            Times.Once);
    }
}
