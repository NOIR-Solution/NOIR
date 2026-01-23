namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for VerifyPasswordResetOtpCommandValidator.
/// Tests validation rules for verifying password reset OTP.
/// </summary>
public class VerifyPasswordResetOtpCommandValidatorTests
{
    private readonly VerifyPasswordResetOtpCommandValidator _validator;

    public VerifyPasswordResetOtpCommandValidatorTests()
    {
        _validator = new VerifyPasswordResetOtpCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");
        mock.Setup(x => x["validation.exactLength"]).Returns("OTP must be exactly 6 characters.");

        return mock.Object;
    }

    #region SessionToken Validation

    [Fact]
    public async Task Validate_WhenSessionTokenIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("", "123456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenSessionTokenIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand(null!, "123456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken);
    }

    [Fact]
    public async Task Validate_WhenSessionTokenIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("   ", "123456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken);
    }

    [Fact]
    public async Task Validate_WhenSessionTokenIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", "123456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SessionToken);
    }

    #endregion

    #region Otp Validation

    [Fact]
    public async Task Validate_WhenOtpIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Otp)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenOtpIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Otp);
    }

    [Theory]
    [InlineData("12345")]     // 5 digits - too short
    [InlineData("1234567")]   // 7 digits - too long
    [InlineData("1234")]      // 4 digits - too short
    [InlineData("12345678")]  // 8 digits - too long
    [InlineData("1")]         // 1 digit - too short
    public async Task Validate_WhenOtpLengthIsIncorrect_ShouldHaveError(string otp)
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", otp);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Otp)
            .WithErrorMessage("OTP must be exactly 6 characters.");
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("abcdef")]  // Letters are allowed by Length rule
    public async Task Validate_WhenOtpLengthIsCorrect_ShouldNotHaveError(string otp)
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", otp);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Otp);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new VerifyPasswordResetOtpCommand("valid-session-token", "123456");

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
        var command = new VerifyPasswordResetOtpCommand("", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        result.ShouldHaveValidationErrorFor(x => x.Otp);
    }

    #endregion
}
