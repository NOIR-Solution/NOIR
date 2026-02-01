namespace NOIR.Application.UnitTests.Features.PlatformSettings.Validators;

/// <summary>
/// Unit tests for TestSmtpConnectionCommandValidator.
/// </summary>
public class TestSmtpConnectionCommandValidatorTests
{
    private readonly TestSmtpConnectionCommandValidator _validator;

    public TestSmtpConnectionCommandValidatorTests()
    {
        _validator = new TestSmtpConnectionCommandValidator();
    }

    #region RecipientEmail Validation

    [Fact]
    public async Task Validate_WhenRecipientEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand(string.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
            .WithErrorMessage("Recipient email is required.");
    }

    [Fact]
    public async Task Validate_WhenRecipientEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail);
    }

    [Fact]
    public async Task Validate_WhenRecipientEmailIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new TestSmtpConnectionCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@domain.com")]
    public async Task Validate_WhenRecipientEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new TestSmtpConnectionCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
            .WithErrorMessage("A valid email address is required.");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co")]
    [InlineData("admin@company.org")]
    public async Task Validate_WhenRecipientEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new TestSmtpConnectionCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
