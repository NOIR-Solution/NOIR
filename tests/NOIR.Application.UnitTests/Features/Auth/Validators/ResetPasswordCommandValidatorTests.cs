namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for ResetPasswordCommandValidator.
/// Tests validation rules for password reset execution.
/// </summary>
public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator;
    private const int MinPasswordLength = 6;

    public ResetPasswordCommandValidatorTests()
    {
        _validator = new ResetPasswordCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");
        mock.Setup(x => x["validation.minLength"]).Returns("Minimum length not met.");

        return mock.Object;
    }

    #region ResetToken Validation

    [Fact]
    public async Task Validate_WhenResetTokenIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand("", "NewPassword123!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResetToken)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenResetTokenIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand(null!, "NewPassword123!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResetToken);
    }

    [Fact]
    public async Task Validate_WhenResetTokenIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand("   ", "NewPassword123!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResetToken);
    }

    [Fact]
    public async Task Validate_WhenResetTokenIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token-abc123", "NewPassword123!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ResetToken);
    }

    #endregion

    #region NewPassword Validation

    [Fact]
    public async Task Validate_WhenNewPasswordIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenNewPasswordIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token", null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Theory]
    [InlineData("12345")]    // 5 chars - too short
    [InlineData("abcde")]    // 5 chars - too short
    [InlineData("Pass")]     // 4 chars - too short
    [InlineData("a")]        // 1 char - too short
    [InlineData("ab")]       // 2 chars - too short
    public async Task Validate_WhenNewPasswordIsTooShort_ShouldHaveError(string password)
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token", password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Minimum length not met.");
    }

    [Theory]
    [InlineData("123456")]          // Exactly 6 chars - minimum valid
    [InlineData("abcdef")]          // Exactly 6 chars - minimum valid
    [InlineData("Password123!")]    // Strong password
    [InlineData("verylongpasswordthatisvalid")] // Long password
    public async Task Validate_WhenNewPasswordMeetsMinimumLength_ShouldNotHaveError(string password)
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token", password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ResetPasswordCommand("valid-reset-token", "NewPassword123!");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenMultipleFieldsAreInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new ResetPasswordCommand("", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResetToken);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion
}
