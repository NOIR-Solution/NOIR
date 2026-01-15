namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending functionality with mocked FluentEmail.
/// Note: SendTemplateAsync tests are integration tests due to DbContext dependency.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IFluentEmail> _fluentEmailMock;
    private readonly Mock<IOptions<EmailSettings>> _emailSettingsMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;

    public EmailServiceTests()
    {
        _fluentEmailMock = new Mock<IFluentEmail>();
        _emailSettingsMock = new Mock<IOptions<EmailSettings>>();
        _emailSettingsMock.Setup(x => x.Value).Returns(new EmailSettings { TemplatesPath = "EmailTemplates" });
        _loggerMock = new Mock<ILogger<EmailService>>();
    }

    private EmailService CreateService(ApplicationDbContext dbContext)
    {
        return new EmailService(
            _fluentEmailMock.Object,
            dbContext,
            _emailSettingsMock.Object,
            _loggerMock.Object);
    }

    private static ApplicationDbContext CreateMockDbContext(List<EmailTemplate>? templates = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new ApplicationDbContext(options, null);

        if (templates != null)
        {
            dbContext.Set<EmailTemplate>().AddRange(templates);
            dbContext.SaveChanges();
        }

        return dbContext;
    }

    #region SendAsync Single Recipient Tests

    [Fact]
    public async Task SendAsync_WithValidEmail_ShouldReturnTrue()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithHtmlBody_ShouldSendAsHtml()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendAsync("test@example.com", "Subject", "<p>Body</p>", isHtml: true);

        // Assert
        result.Should().BeTrue();
        _fluentEmailMock.Verify(x => x.Body(It.IsAny<string>(), true), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithPlainTextBody_ShouldSendAsPlainText()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendAsync("test@example.com", "Subject", "Plain text body", isHtml: false);

        // Assert
        result.Should().BeTrue();
        _fluentEmailMock.Verify(x => x.Body(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenSendFails_ShouldReturnFalse()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse();
        response.ErrorMessages.Add("SMTP error");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Throws(new Exception("Network error"));

        // Act
        var result = await sut.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Throws(new Exception("Network error"));

        // Act
        await sut.SendAsync("test@example.com", "Subject", "Body");

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
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_ToMultipleRecipients_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        var response = new SendResponse();
        response.ErrorMessages.Add("SMTP error");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), It.IsAny<bool>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ToMultipleRecipients_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        _fluentEmailMock.Setup(x => x.To(It.IsAny<IEnumerable<Address>>()))
            .Throws(new Exception("Network error"));

        var recipients = new[] { "test1@example.com", "test2@example.com" };

        // Act
        var result = await sut.SendAsync(recipients, "Subject", "Body");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SendTemplateAsync Tests

    [Fact]
    public async Task SendTemplateAsync_WithValidTemplate_ShouldReturnTrue()
    {
        // Arrange
        var template = EmailTemplate.Create(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");

        using var dbContext = CreateMockDbContext([template]);
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "TestTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenTemplateNotFound_ShouldReturnFalse()
    {
        // Arrange
        using var dbContext = CreateMockDbContext();
        var sut = CreateService(dbContext);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "NonExistentTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenTemplateNotActive_ShouldReturnFalse()
    {
        // Arrange
        var template = EmailTemplate.Create(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");
        template.Deactivate();

        using var dbContext = CreateMockDbContext([template]);
        var sut = CreateService(dbContext);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "TestTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenSendFails_ShouldReturnFalse()
    {
        // Arrange
        var template = EmailTemplate.Create(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");

        using var dbContext = CreateMockDbContext([template]);
        var sut = CreateService(dbContext);

        var response = new SendResponse();
        response.ErrorMessages.Add("SMTP error");
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "TestTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_ShouldReplacePlaceholders()
    {
        // Arrange
        var template = EmailTemplate.Create(
            name: "TestTemplate",
            subject: "Hello {{Name}}",
            htmlBody: "<p>Hello {{Name}}, your email is {{Email}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\", \"Email\"]");

        using var dbContext = CreateMockDbContext([template]);
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        string? capturedBody = null;
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true))
            .Callback<string, bool>((body, _) => capturedBody = body)
            .Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await sut.SendTemplateAsync("test@example.com", "", "TestTemplate", new { Name = "John", Email = "john@test.com" });

        // Assert
        capturedBody.Should().Contain("Hello John");
        capturedBody.Should().Contain("john@test.com");
    }

    [Fact]
    public async Task SendTemplateAsync_WithPlatformTemplate_ShouldUseFallback()
    {
        // Arrange - Platform template (TenantId = null)
        var platformTemplate = EmailTemplate.Create(
            name: "WelcomeEmail",
            subject: "Welcome",
            htmlBody: "<p>Welcome {{Name}}</p>",
            tenantId: null); // Platform-level template

        using var dbContext = CreateMockDbContext([platformTemplate]);
        var sut = CreateService(dbContext);

        var response = new SendResponse { MessageId = "123" };
        _fluentEmailMock.Setup(x => x.To(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Subject(It.IsAny<string>())).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.Body(It.IsAny<string>(), true)).Returns(_fluentEmailMock.Object);
        _fluentEmailMock.Setup(x => x.SendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Welcome", "WelcomeEmail", new { Name = "Test" });

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
