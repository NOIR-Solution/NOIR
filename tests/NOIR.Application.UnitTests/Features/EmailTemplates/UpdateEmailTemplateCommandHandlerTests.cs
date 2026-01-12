using NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;
using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for UpdateEmailTemplateCommandHandler.
/// Tests email template update scenarios.
/// </summary>
public class UpdateEmailTemplateCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateEmailTemplateCommandHandler _handler;

    public UpdateEmailTemplateCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateEmailTemplateCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string language = "en",
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
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Old Subject",
            htmlBody: "<p>Old Body</p>");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            "New plain text",
            "Updated description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("New Subject");
        result.Value.HtmlBody.Should().Be("<p>New Body</p>");
        result.Value.PlainTextBody.Should().Be("New plain text");
        result.Value.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_ShouldIncrementVersion()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(2); // Version should be incremented from 1 to 2
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPreserveExistingVariables()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            availableVariables: "[\"UserName\", \"OtpCode\"]");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableVariables.Should().BeEquivalentTo(new[] { "UserName", "OtpCode" });
    }

    [Fact]
    public async Task Handle_WithNullPlainTextBody_ShouldUpdateSuccessfully()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,  // No plain text body
            null); // No description

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlainTextBody.Should().BeNull();
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

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                token))
            .ReturnsAsync(template);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            templateId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<EmailTemplate>>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}
