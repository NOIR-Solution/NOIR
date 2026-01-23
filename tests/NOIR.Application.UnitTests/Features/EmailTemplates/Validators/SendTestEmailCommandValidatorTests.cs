namespace NOIR.Application.UnitTests.Features.EmailTemplates.Validators;

using NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;

/// <summary>
/// Unit tests for SendTestEmailCommandValidator.
/// Tests validation rules for sending a test email.
/// </summary>
public class SendTestEmailCommandValidatorTests
{
    private readonly SendTestEmailCommandValidator _validator;

    public SendTestEmailCommandValidatorTests()
    {
        _validator = new SendTestEmailCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>
            {
                { "Name", "John Doe" },
                { "Subject", "Test Subject" }
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSampleDataIsEmpty_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TemplateId Validation

    [Fact]
    public async Task Validate_WhenTemplateIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.Empty,
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("Template ID is required.");
    }

    [Fact]
    public async Task Validate_WhenTemplateIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TemplateId);
    }

    #endregion

    #region RecipientEmail Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenRecipientEmailIsEmptyOrWhitespace_ShouldHaveError(string? email)
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: email!,
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("invalid email")]
    public async Task Validate_WhenRecipientEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: email,
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
            .WithErrorMessage("Invalid email address format.");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.org")]
    [InlineData("admin@subdomain.example.co.uk")]
    public async Task Validate_WhenRecipientEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: email,
            SampleData: new Dictionary<string, string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RecipientEmail);
    }

    #endregion

    #region SampleData Validation

    [Fact]
    public async Task Validate_WhenSampleDataIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new SendTestEmailCommand(
            TemplateId: Guid.NewGuid(),
            RecipientEmail: "test@example.com",
            SampleData: null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SampleData)
            .WithErrorMessage("Sample data is required.");
    }

    #endregion
}
