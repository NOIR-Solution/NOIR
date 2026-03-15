using NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;
using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Domain.Entities;

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for SendTestEmailCommandHandler.
/// Tests test email sending scenarios.
/// </summary>
public class SendTestEmailCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<SendTestEmailCommandHandler>> _loggerMock;
    private readonly SendTestEmailCommandHandler _handler;

    public SendTestEmailCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<SendTestEmailCommandHandler>>();

        _handler = new SendTestEmailCommandHandler(
            _repositoryMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject {{UserName}}",
        string htmlBody = "<p>Hello, {{UserName}}!</p>",
        string? plainTextBody = "Hello, {{UserName}}!")
    {
        return EmailTemplate.CreatePlatformDefault(
            name,
            subject,
            htmlBody,
            plainTextBody,
            description: "Test Description",
            availableVariables: "[\"UserName\"]");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSendEmail()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            subject: "Welcome {{UserName}}",
            htmlBody: "<p>Welcome, {{UserName}}!</p>",
            plainTextBody: "Welcome, {{UserName}}!");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string> { { "UserName", "John" } });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Subject.ShouldBe("Welcome John");
        result.Value.HtmlBody.ShouldBe("<p>Welcome, John!</p>");
        result.Value.PlainTextBody.ShouldBe("Welcome, John!");
    }

    [Fact]
    public async Task Handle_ShouldReplaceDoubleAndSingleBraceVariables()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            subject: "Hello {{UserName}} and {OtpCode}",
            htmlBody: "<p>{{UserName}}, your code is {OtpCode}</p>");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>
            {
                { "UserName", "John" },
                { "OtpCode", "123456" }
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Subject.ShouldBe("Hello John and 123456");
        result.Value.HtmlBody.ShouldBe("<p>John, your code is 123456</p>");
    }

    [Fact]
    public async Task Handle_WithNullPlainTextBody_ShouldReturnNullPlainText()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(plainTextBody: null);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string> { { "UserName", "John" } });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.PlainTextBody.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallEmailServiceWithCorrectParameters()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            subject: "Test Subject",
            htmlBody: "<p>Test Body</p>");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                "recipient@test.com",
                "Test Subject",
                "<p>Test Body</p>",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "recipient@test.com",
            new Dictionary<string, string>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendAsync(
                "recipient@test.com",
                "Test Subject",
                "<p>Test Body</p>",
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-EMAIL-001");
        _emailServiceMock.Verify(
            x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Email Send Failure Scenarios

    [Fact]
    public async Task Handle_WhenEmailSendFails_ShouldReturnFailure()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-EMAIL-002");
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-EMAIL-003");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptySampleData_ShouldNotReplaceVariables()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var template = CreateTestEmailTemplate(
            subject: "Hello {{UserName}}",
            htmlBody: "<p>Hello {{UserName}}</p>");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<EmailTemplate>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>()); // Empty sample data

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Subject.ShouldBe("Hello {{UserName}}"); // Variables not replaced
        result.Value.HtmlBody.ShouldBe("<p>Hello {{UserName}}</p>");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
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

        _emailServiceMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                token))
            .ReturnsAsync(true);

        var command = new SendTestEmailCommand(
            templateId,
            "test@example.com",
            new Dictionary<string, string>());

        // Act
        await _handler.Handle(command, token);

        // Assert
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<EmailTemplate>>(), token),
            Times.Once);
        _emailServiceMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true, token),
            Times.Once);
    }

    #endregion
}
