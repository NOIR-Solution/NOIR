using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for GetEmailTemplateQueryHandler.
/// Tests single email template retrieval scenarios with Copy-on-Write pattern.
/// </summary>
public class GetEmailTemplateQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetEmailTemplateQueryHandler _handler;

    public GetEmailTemplateQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetEmailTemplateQueryHandler(
            _dbContextMock.Object,
            _currentUserMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string? plainTextBody = "Test Body",
        string? description = "Test Description",
        string? availableVariables = null,
        string? tenantId = null)
    {
        return tenantId == null
            ? EmailTemplate.CreatePlatformDefault(
                name,
                subject,
                htmlBody,
                plainTextBody,
                description,
                availableVariables)
            : EmailTemplate.CreateTenantOverride(
                tenantId,
                name,
                subject,
                htmlBody,
                plainTextBody,
                description,
                availableVariables);
    }

    private void SetupDbContextWithTemplates(params EmailTemplate[] templates)
    {
        var data = templates.AsQueryable();
        var mockSet = new Mock<DbSet<EmailTemplate>>();

        mockSet.As<IAsyncEnumerable<EmailTemplate>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<EmailTemplate>(data.GetEnumerator()));

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<EmailTemplate>(data.Provider));

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.Expression)
            .Returns(data.Expression);

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.ElementType)
            .Returns(data.ElementType);

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.GetEnumerator())
            .Returns(data.GetEnumerator());

        _dbContextMock.Setup(x => x.EmailTemplates).Returns(mockSet.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        var template = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Welcome to Our Service",
            htmlBody: "<h1>Welcome, {{UserName}}!</h1>",
            tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("WelcomeEmail");
        result.Value.Subject.ShouldBe("Welcome to Our Service");
        result.Value.HtmlBody.ShouldBe("<h1>Welcome, {{UserName}}!</h1>");
        result.Value.IsInherited.ShouldBe(false); // Tenant-owned template
    }

    [Fact]
    public async Task Handle_WithPlatformTemplate_ShouldReturnAsInherited()
    {
        // Arrange
        var template = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Welcome to Our Service",
            htmlBody: "<h1>Welcome!</h1>",
            tenantId: null); // Platform template

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsInherited.ShouldBe(true); // Platform template viewed by tenant user
    }

    [Fact]
    public async Task Handle_WithTemplateWithVariables_ShouldParseVariables()
    {
        // Arrange
        var template = CreateTestEmailTemplate(
            name: "PasswordReset",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]",
            tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.AvailableVariables.ShouldBe(new[] { "UserName", "OtpCode", "ExpiryMinutes" });
    }

    [Fact]
    public async Task Handle_WithTemplateWithoutVariables_ShouldReturnEmptyList()
    {
        // Arrange
        var template = CreateTestEmailTemplate(availableVariables: null, tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.AvailableVariables.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidJsonVariables_ShouldReturnEmptyList()
    {
        // Arrange
        var template = CreateTestEmailTemplate(availableVariables: "invalid json", tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.AvailableVariables.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_PlatformAdminViewingPlatformTemplate_ShouldNotBeInherited()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform admin has no tenant context

        var template = CreateTestEmailTemplate(
            name: "PlatformTemplate",
            tenantId: null);

        SetupDbContextWithTemplates(template);

        var query = new GetEmailTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsInherited.ShouldBe(false); // Platform admin viewing platform template
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbContextWithTemplates(); // Empty

        var query = new GetEmailTemplateQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-EMAIL-001");
    }

    #endregion
}
