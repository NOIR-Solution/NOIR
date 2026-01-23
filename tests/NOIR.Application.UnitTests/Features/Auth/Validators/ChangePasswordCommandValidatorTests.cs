namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for ChangePasswordCommandValidator.
/// Tests validation rules for password change operation.
/// </summary>
public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator;
    private const int MinPasswordLength = 6;

    public ChangePasswordCommandValidatorTests()
    {
        _validator = new ChangePasswordCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Password validations
        mock.Setup(x => x["validation.currentPassword.required"]).Returns("Current password is required.");
        mock.Setup(x => x["validation.newPassword.required"]).Returns("New password is required.");
        mock.Setup(x => x.Get("validation.password.tooShort", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Password must be at least {args[0]} characters.");
        mock.Setup(x => x["validation.password.mustBeDifferent"]).Returns("New password must be different from current password.");

        return mock.Object;
    }

    #region CurrentPassword Validation

    [Fact]
    public async Task Validate_WhenCurrentPasswordIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("", "NewPassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
            .WithErrorMessage("Current password is required.");
    }

    [Fact]
    public async Task Validate_WhenCurrentPasswordIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand(null!, "NewPassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public async Task Validate_WhenCurrentPasswordIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("   ", "NewPassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public async Task Validate_WhenCurrentPasswordIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", "NewPassword456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CurrentPassword);
    }

    #endregion

    #region NewPassword Validation

    [Fact]
    public async Task Validate_WhenNewPasswordIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password is required.");
    }

    [Fact]
    public async Task Validate_WhenNewPasswordIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", null!);

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
    public async Task Validate_WhenNewPasswordIsTooShort_ShouldHaveError(string password)
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage($"Password must be at least {MinPasswordLength} characters.");
    }

    [Theory]
    [InlineData("123456")]          // Exactly 6 chars - minimum valid
    [InlineData("abcdef")]          // Exactly 6 chars - minimum valid
    [InlineData("Password123!")]    // Strong password
    [InlineData("verylongpasswordthatisvalid")] // Long password
    public async Task Validate_WhenNewPasswordMeetsMinimumLength_ShouldNotHaveError(string password)
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion

    #region Password Must Be Different Validation

    [Fact]
    public async Task Validate_WhenNewPasswordEqualsCurrentPassword_ShouldHaveError()
    {
        // Arrange
        var samePassword = "SamePassword123";
        var command = new ChangePasswordCommand(samePassword, samePassword);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be different from current password.");
    }

    [Fact]
    public async Task Validate_WhenNewPasswordDiffersFromCurrentPassword_ShouldNotHaveError()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", "NewPassword456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPassword123", "NewPassword456");

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
        var command = new ChangePasswordCommand("", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion
}
