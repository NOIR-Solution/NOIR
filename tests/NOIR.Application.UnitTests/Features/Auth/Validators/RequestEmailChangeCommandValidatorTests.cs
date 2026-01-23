namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for RequestEmailChangeCommandValidator.
/// Tests validation rules for email change request.
/// </summary>
public class RequestEmailChangeCommandValidatorTests
{
    private readonly RequestEmailChangeCommandValidator _validator;

    public RequestEmailChangeCommandValidatorTests()
    {
        _validator = new RequestEmailChangeCommandValidator(CreateLocalizationMock());
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

    #region NewEmail Validation - Empty/Null

    [Fact]
    public async Task Validate_WhenNewEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RequestEmailChangeCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenNewEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new RequestEmailChangeCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail);
    }

    [Fact]
    public async Task Validate_WhenNewEmailIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new RequestEmailChangeCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail);
    }

    #endregion

    #region NewEmail Validation - Max Length

    [Fact]
    public async Task Validate_WhenNewEmailExceeds256Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // 259 characters
        var command = new RequestEmailChangeCommand(longEmail);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Maximum length exceeded.");
    }

    [Fact]
    public async Task Validate_WhenNewEmailIsExactly256Characters_ShouldNotHaveMaxLengthError()
    {
        // Arrange
        var maxEmail = new string('a', 240) + "@test.com"; // Just under 256 characters
        var command = new RequestEmailChangeCommand(maxEmail);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
    }

    #endregion

    #region NewEmail Validation - Email Format

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("double@@at.com")]
    public async Task Validate_WhenNewEmailFormatIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new RequestEmailChangeCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail)
            .WithErrorMessage("Invalid email format.");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@sub.domain.com")]
    [InlineData("test123@company.co.uk")]
    public async Task Validate_WhenNewEmailFormatIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new RequestEmailChangeCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RequestEmailChangeCommand("newemail@example.com");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
