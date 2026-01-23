namespace NOIR.Application.UnitTests.Features.EmailTemplates.Validators;

using NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Unit tests for UpdateEmailTemplateCommandValidator.
/// Tests validation rules for updating email templates.
/// </summary>
public class UpdateEmailTemplateCommandValidatorTests
{
    private readonly UpdateEmailTemplateCommandValidator _validator;

    public UpdateEmailTemplateCommandValidatorTests()
    {
        _validator = new UpdateEmailTemplateCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Hello {{Name}}</body></html>",
            PlainTextBody: "Hello {{Name}}",
            Description: "Test email template description");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandHasMinimalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.Empty,
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Template ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Subject Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenSubjectIsEmptyOrWhitespace_ShouldHaveError(string? subject)
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: subject!,
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public async Task Validate_WhenSubjectExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: new string('a', 501),
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Subject)
            .WithErrorMessage("Subject cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenSubjectIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: new string('a', 500),
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Subject);
    }

    [Fact]
    public async Task Validate_WhenSubjectHasTemplateVariables_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Welcome {{UserName}} to {{CompanyName}}!",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Subject);
    }

    #endregion

    #region HtmlBody Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenHtmlBodyIsEmptyOrWhitespace_ShouldHaveError(string? htmlBody)
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: htmlBody!,
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HtmlBody)
            .WithErrorMessage("HTML body is required.");
    }

    [Fact]
    public async Task Validate_WhenHtmlBodyIsLarge_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>" + new string('a', 100000) + "</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HtmlBody);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Validate_WhenDescriptionExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: new string('a', 1001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIs1000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateEmailTemplateCommand(
            Id: Guid.NewGuid(),
            Subject: "Test Subject",
            HtmlBody: "<html><body>Content</body></html>",
            PlainTextBody: null,
            Description: new string('a', 1000));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion
}
