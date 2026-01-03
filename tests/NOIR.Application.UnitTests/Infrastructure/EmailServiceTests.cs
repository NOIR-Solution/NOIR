namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending functionality with mocked FluentEmail.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IFluentEmail> _fluentEmailMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _sut;

    public EmailServiceTests()
    {
        _fluentEmailMock = new Mock<IFluentEmail>();
        _loggerMock = new Mock<ILogger<EmailService>>();
        _sut = new EmailService(_fluentEmailMock.Object, _loggerMock.Object);
    }

    #region SendAsync Single Recipient Tests

    [Fact]
    public async Task SendAsync_WithValidEmail_ShouldReturnTrue()
    {
        // Arrange
        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithHtmlBody_ShouldSendAsHtml()
    {
        // Arrange
        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _sut.SendAsync("test@example.com", "Subject", "<p>HTML Body</p>", isHtml: true);

        // Assert
        _fluentEmailMock.Verify(x => x.Body(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithPlainTextBody_ShouldSendAsPlainText()
    {
        // Arrange
        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), false)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _sut.SendAsync("test@example.com", "Subject", "Plain text body", isHtml: false);

        // Assert
        _fluentEmailMock.Verify(x => x.Body("Plain text body", false), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenSendFails_ShouldReturnFalse()
    {
        // Arrange
        var response = new SendResponse();
        response.ErrorMessages.Add("SMTP error");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Throws(new Exception("Network error"));

        // Act
        var result = await _sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Throws(new Exception("Network error"));

        // Act
        await _sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendAsync Multiple Recipients Tests

    [Fact]
    public async Task SendAsync_ToMultipleRecipients_ShouldReturnTrue()
    {
        // Arrange
        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await _sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ToMultipleRecipients_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        var response = new SendResponse();
        response.ErrorMessages.Add("SMTP error");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await _sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ToMultipleRecipients_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>()))
            .Throws(new Exception("Network error"));

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await _sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendTemplateAsync Tests

    [Fact]
    public async Task SendTemplateAsync_WithValidTemplate_ShouldReturnTrue()
    {
        // Arrange
        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.UsingTemplateFromFile(It.IsAny<string>(), It.IsAny<object>(), true))
            .Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SendTemplateAsync("test@example.com", "Subject", "template.html", new { Name = "Test" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        var response = new SendResponse();
        response.ErrorMessages.Add("Template not found");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.UsingTemplateFromFile(It.IsAny<string>(), It.IsAny<object>(), true))
            .Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.SendTemplateAsync("test@example.com", "Subject", "template.html", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Throws(new Exception("Template error"));

        // Act
        var result = await _sut.SendTemplateAsync("test@example.com", "Subject", "template.html", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
