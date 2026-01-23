namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for LoginCommandValidator.
/// Tests validation rules for user login.
/// </summary>
public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Email validations
        mock.Setup(x => x["validation.email.required"]).Returns("Email is required.");
        mock.Setup(x => x["validation.email.invalid"]).Returns("Invalid email format.");

        // Password validations
        mock.Setup(x => x["validation.password.required"]).Returns("Password is required.");

        return mock.Object;
    }

    #region Email Validation

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("", "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand(null!, "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("double@@at.com")]
    public async Task Validate_WhenEmailFormatIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "password123");

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
        var command = new LoginCommand(email, "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Validation

    [Fact]
    public async Task Validate_WhenPasswordIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public async Task Validate_WhenPasswordIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("   ")]
    public async Task Validate_WhenPasswordIsWhitespace_ShouldHaveError(string password)
    {
        // Arrange
        var command = new LoginCommand("user@example.com", password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WhenPasswordIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "password123", UseCookies: true, TenantId: "tenant-1");

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
        var command = new LoginCommand("", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    #endregion
}
