namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

/// <summary>
/// Unit tests for UpdateContactSettingsCommandValidator.
/// Tests validation rules for updating contact settings.
/// </summary>
public class UpdateContactSettingsCommandValidatorTests
{
    private readonly UpdateContactSettingsCommandValidator _validator;

    public UpdateContactSettingsCommandValidatorTests()
    {
        _validator = new UpdateContactSettingsCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: "contact@example.com",
            Phone: "+1-555-0100",
            Address: "123 Main St, City, Country");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreEmpty_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: "",
            Phone: "",
            Address: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Email Validation

    [Fact]
    public async Task Validate_WhenEmailExceeds256Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: new string('a', 245) + "@example.com", // 257 chars
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 256 characters.");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WhenEmailHasInvalidFormat_ShouldHaveError(string email)
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: email,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Theory]
    [InlineData("contact@example.com")]
    [InlineData("user.name@example.org")]
    public async Task Validate_WhenEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: email,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Validate_WhenEmailIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Phone Validation

    [Fact]
    public async Task Validate_WhenPhoneExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: new string('1', 51),
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Phone must not exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_WhenPhoneIs50Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: new string('1', 50),
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public async Task Validate_WhenPhoneIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    #endregion

    #region Address Validation

    [Fact]
    public async Task Validate_WhenAddressExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenAddressIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: new string('a', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public async Task Validate_WhenAddressIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateContactSettingsCommand(
            Email: null,
            Phone: null,
            Address: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    #endregion
}
