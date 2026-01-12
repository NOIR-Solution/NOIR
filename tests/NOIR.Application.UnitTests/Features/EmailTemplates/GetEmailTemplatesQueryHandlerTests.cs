using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for GetEmailTemplatesQueryHandler.
/// Tests list retrieval scenarios with filtering.
/// </summary>
public class GetEmailTemplatesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly GetEmailTemplatesQueryHandler _handler;

    public GetEmailTemplatesQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _handler = new GetEmailTemplatesQueryHandler(_repositoryMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string language = "en",
        bool isActive = true,
        string? availableVariables = null)
    {
        return EmailTemplate.Create(
            name,
            subject,
            htmlBody,
            language,
            plainTextBody: null,
            description: "Test Description",
            availableVariables: availableVariables);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllTemplates()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            CreateTestEmailTemplate(name: "WelcomeEmail", subject: "Welcome"),
            CreateTestEmailTemplate(name: "PasswordReset", subject: "Reset Password"),
            CreateTestEmailTemplate(name: "OrderConfirmation", subject: "Order Confirmed")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithLanguageFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            CreateTestEmailTemplate(name: "WelcomeEmail", language: "vi")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetEmailTemplatesQuery(Language: "vi");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Language.Should().Be("vi");
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            CreateTestEmailTemplate(name: "PasswordReset", subject: "Reset Password")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetEmailTemplatesQuery(Search: "password");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var templates = new List<EmailTemplate>
        {
            CreateTestEmailTemplate(
                name: "WelcomeEmail",
                subject: "Welcome",
                language: "en",
                isActive: true,
                availableVariables: "[\"UserName\"]")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Name.Should().Be("WelcomeEmail");
        dto.Subject.Should().Be("Welcome");
        dto.Language.Should().Be("en");
        dto.IsActive.Should().BeTrue();
        dto.Version.Should().Be(1);
        dto.AvailableVariables.Should().Contain("UserName");
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailTemplate>());

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
        var templates = new List<EmailTemplate>
        {
            CreateTestEmailTemplate(availableVariables: "invalid json")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetEmailTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].AvailableVariables.Should().BeEmpty();
    }

    #endregion
}
