using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for GetEmailTemplateQueryHandler.
/// Tests single email template retrieval scenarios.
/// </summary>
public class GetEmailTemplateQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly GetEmailTemplateQueryHandler _handler;

    public GetEmailTemplateQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _handler = new GetEmailTemplateQueryHandler(_repositoryMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        Guid? id = null,
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string? plainTextBody = "Test Body",
        string language = "en",
        bool isActive = true,
        int version = 1,
        string? description = "Test Description",
        string? availableVariables = null)
    {
        return EmailTemplate.Create(
            name,
            subject,
            htmlBody,
            language,
            plainTextBody,
            description,
            availableVariables);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Welcome to Our Service",
            htmlBody: "<h1>Welcome, {{UserName}}!</h1>");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var query = new GetEmailTemplateQuery(templateId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("WelcomeEmail");
        result.Value.Subject.Should().Be("Welcome to Our Service");
        result.Value.HtmlBody.Should().Be("<h1>Welcome, {{UserName}}!</h1>");
    }

    [Fact]
    public async Task Handle_WithTemplateWithVariables_ShouldParseVariables()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            name: "PasswordReset",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var query = new GetEmailTemplateQuery(templateId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableVariables.Should().BeEquivalentTo(new[] { "UserName", "OtpCode", "ExpiryMinutes" });
    }

    [Fact]
    public async Task Handle_WithTemplateWithoutVariables_ShouldReturnEmptyList()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(availableVariables: null);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var query = new GetEmailTemplateQuery(templateId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableVariables.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidJsonVariables_ShouldReturnEmptyList()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(availableVariables: "invalid json");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var query = new GetEmailTemplateQuery(templateId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableVariables.Should().BeEmpty();
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        var query = new GetEmailTemplateQuery(templateId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
    }

    #endregion
}
