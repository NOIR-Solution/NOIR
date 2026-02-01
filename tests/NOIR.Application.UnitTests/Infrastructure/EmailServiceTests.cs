namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending functionality with mocked FluentEmail and repository.
/// </summary>
public class EmailServiceTests
{
    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IFluentEmail> _fluentEmailMock;
    private readonly Mock<IRepository<EmailTemplate, Guid>> _emailTemplateRepositoryMock;
    private readonly Mock<IOptionsMonitor<EmailSettings>> _emailSettingsMock;
    private readonly Mock<ITenantSettingsService> _tenantSettingsMock;
    private readonly Mock<IMultiTenantContextAccessor<Tenant>> _tenantContextAccessorMock;
    private readonly IFusionCache _cache;
    private readonly Mock<ILogger<EmailService>> _loggerMock;

    public EmailServiceTests()
    {
        _fluentEmailMock = new Mock<IFluentEmail>();
        _emailTemplateRepositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _emailSettingsMock = new Mock<IOptionsMonitor<EmailSettings>>();
        _emailSettingsMock.Setup(x => x.CurrentValue).Returns(new EmailSettings { TemplatesPath = "EmailTemplates" });
        _tenantSettingsMock = new Mock<ITenantSettingsService>();
        // Setup for both tenant-level and platform-level SMTP settings queries (return empty = no DB settings configured)
        _tenantSettingsMock.Setup(x => x.GetSettingsAsync(It.IsAny<string?>(), "smtp:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>().AsReadOnly());
        _tenantContextAccessorMock = new Mock<IMultiTenantContextAccessor<Tenant>>();
        // Use real FusionCache for tests - lightweight in-memory cache with no external dependencies
        _cache = new FusionCache(new FusionCacheOptions { DefaultEntryOptions = new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(1) } });
        _loggerMock = new Mock<ILogger<EmailService>>();
    }

    private EmailService CreateService(List<EmailTemplate>? templates = null)
    {
        // Setup repository to return templates when ListAsync is called with any specification
        var templateList = templates ?? new List<EmailTemplate>();
        _emailTemplateRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<EmailTemplate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISpecification<EmailTemplate> spec, CancellationToken _) =>
            {
                // Simulate the spec filtering - return templates as-is since spec is already applied in production
                return (IReadOnlyList<EmailTemplate>)templateList;
            });

        return new EmailService(
            _fluentEmailMock.Object,
            _emailTemplateRepositoryMock.Object,
            _emailSettingsMock.Object,
            _tenantSettingsMock.Object,
            _tenantContextAccessorMock.Object,
            _cache,
            _loggerMock.Object);
    }

    private void SetupTenantContext(string? tenantId)
    {
        if (tenantId != null)
        {
            var mockTenantContext = new Mock<IMultiTenantContext<Tenant>>();
            var testTenant = new Tenant(tenantId, "test-tenant", "Test Tenant");
            mockTenantContext.Setup(x => x.TenantInfo).Returns(testTenant);
            _tenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns(mockTenantContext.Object);
        }
        else
        {
            _tenantContextAccessorMock.Setup(x => x.MultiTenantContext).Returns((IMultiTenantContext<Tenant>?)null!);
        }
    }

    #region SendAsync Single Recipient Tests

    [Fact]
    public async Task SendAsync_WithValidEmail_ShouldReturnTrue()
    {
        // Arrange
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var sut = CreateService();

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
        var template = EmailTemplate.CreatePlatformDefault(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");

        SetupTenantContext(TestTenantId);
        var sut = CreateService([template]);

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
        // Arrange - empty template list
        var sut = CreateService();

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "NonExistentTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenTemplateNotActive_ShouldReturnFalse()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");
        template.Deactivate();

        SetupTenantContext(TestTenantId);
        var sut = CreateService([template]);

        // Act
        var result = await sut.SendTemplateAsync("test@example.com", "Subject", "TestTemplate", new { Name = "Test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplateAsync_WhenSendFails_ShouldReturnFalse()
    {
        // Arrange
        var template = EmailTemplate.CreatePlatformDefault(
            name: "TestTemplate",
            subject: "Test Subject",
            htmlBody: "<p>Hello {{Name}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\"]");

        SetupTenantContext(TestTenantId);
        var sut = CreateService([template]);

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
        var template = EmailTemplate.CreatePlatformDefault(
            name: "TestTemplate",
            subject: "Hello {{Name}}",
            htmlBody: "<p>Hello {{Name}}, your email is {{Email}}</p>",
            plainTextBody: "Hello {{Name}}",
            description: "Test template",
            availableVariables: "[\"Name\", \"Email\"]");

        SetupTenantContext(TestTenantId);
        var sut = CreateService([template]);

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
        // Arrange - Platform template (TenantId = null for platform-level fallback)
        var platformTemplate = EmailTemplate.CreatePlatformDefault(
            name: "WelcomeEmail",
            subject: "Welcome",
            htmlBody: "<p>Welcome {{Name}}</p>");

        // Setup tenant context - current tenant is different, so fallback to platform template
        SetupTenantContext(TestTenantId);
        var sut = CreateService([platformTemplate]);

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
