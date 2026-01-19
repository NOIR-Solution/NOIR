using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for GetEmailTemplatesQueryHandler.
/// Tests list retrieval scenarios with Copy-on-Write pattern.
/// </summary>
public class GetEmailTemplatesQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetEmailTemplatesQueryHandler _handler;

    public GetEmailTemplatesQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetEmailTemplatesQueryHandler(
            _dbContextMock.Object,
            _currentUserMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string? availableVariables = null,
        string? tenantId = null)
    {
        return EmailTemplate.Create(
            name,
            subject,
            htmlBody,
            plainTextBody: null,
            description: "Test Description",
            availableVariables: availableVariables,
            tenantId: tenantId);
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
    public async Task Handle_WithNoFilters_ShouldReturnAllTemplates()
    {
        // Arrange - mix of platform and tenant templates
        var templates = new[]
        {
            CreateTestEmailTemplate(name: "TenantEmail", subject: "Tenant Subject", tenantId: TestTenantId),
            CreateTestEmailTemplate(name: "PlatformEmail", subject: "Platform Subject", tenantId: null)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_TenantTemplateOverridesPlatformTemplate_ShouldOnlyShowTenantVersion()
    {
        // Arrange - same template name with both platform and tenant version
        var templates = new[]
        {
            CreateTestEmailTemplate(name: "WelcomeEmail", subject: "Platform Welcome", tenantId: null),
            CreateTestEmailTemplate(name: "WelcomeEmail", subject: "Tenant Welcome", tenantId: TestTenantId)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1); // Only one "WelcomeEmail" should be returned
        result.Value[0].Subject.Should().Be("Tenant Welcome");
        result.Value[0].IsInherited.Should().BeFalse(); // Tenant owns it
    }

    [Fact]
    public async Task Handle_PlatformTemplateWithNoTenantOverride_ShouldShowAsInherited()
    {
        // Arrange - only platform template exists
        var templates = new[]
        {
            CreateTestEmailTemplate(name: "WelcomeEmail", subject: "Platform Welcome", tenantId: null)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].IsInherited.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var templates = new[]
        {
            CreateTestEmailTemplate(
                name: "WelcomeEmail",
                subject: "Welcome",
                availableVariables: "[\"UserName\"]",
                tenantId: TestTenantId)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Name.Should().Be("WelcomeEmail");
        dto.Subject.Should().Be("Welcome");
        dto.IsActive.Should().BeTrue();
        dto.Version.Should().Be(1);
        dto.AvailableVariables.Should().Contain("UserName");
        dto.IsInherited.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        SetupDbContextWithTemplates(); // Empty

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidJsonVariables_ShouldReturnEmptyVariablesList()
    {
        // Arrange
        var templates = new[]
        {
            CreateTestEmailTemplate(availableVariables: "invalid json", tenantId: TestTenantId)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].AvailableVariables.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnTemplatesOrderedByName()
    {
        // Arrange
        var templates = new[]
        {
            CreateTestEmailTemplate(name: "ZLastTemplate", tenantId: TestTenantId),
            CreateTestEmailTemplate(name: "AFirstTemplate", tenantId: TestTenantId),
            CreateTestEmailTemplate(name: "MMiddleTemplate", tenantId: TestTenantId)
        };

        SetupDbContextWithTemplates(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Name.Should().Be("AFirstTemplate");
        result.Value[1].Name.Should().Be("MMiddleTemplate");
        result.Value[2].Name.Should().Be("ZLastTemplate");
    }

    #endregion
}
