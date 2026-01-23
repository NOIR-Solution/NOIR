namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for RequestPasswordResetCommandValidator.
/// Tests validation rules for password reset request.
/// </summary>
public class RequestPasswordResetCommandValidatorTests
{
    private readonly RequestPasswordResetCommandValidator _validator;

    public RequestPasswordResetCommandValidatorTests()
    {
        _validator = new RequestPasswordResetCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");
        mock.Setup(x => x["validation.maxLength"]).Returns("Maximum length exceeded.");
        mock.Setup(x => x["validation.email"]).Returns("Invalid email format.");

        return mock.Object;
    }

    #region Email Validation - Empty/Null

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new RequestPasswordResetCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Email Validation - Max Length

    [Fact]
    public async Task Validate_WhenEmailExceeds256Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // 259 characters
        var command = new RequestPasswordResetCommand(longEmail);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Maximum length exceeded.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsUnder256Characters_ShouldNotHaveMaxLengthError()
    {
        // Arrange
        var maxEmail = new string('a', 240) + "@test.com"; // Just under 256 characters
        var command = new RequestPasswordResetCommand(maxEmail);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Email Validation - Format

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("double@@at.com")]
    public async Task Validate_WhenEmailFormatIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@sub.domain.com")]
    [InlineData("test123@company.co.uk")]
    public async Task Validate_WhenEmailFormatIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new RequestPasswordResetCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RequestPasswordResetCommand("user@example.com");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
