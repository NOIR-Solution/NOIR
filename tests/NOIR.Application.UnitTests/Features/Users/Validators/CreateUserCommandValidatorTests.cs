namespace NOIR.Application.UnitTests.Features.Users.Validators;

/// <summary>
/// Unit tests for CreateUserCommandValidator.
/// Tests all validation rules for creating a user.
/// </summary>
public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator;

    public CreateUserCommandValidatorTests()
    {
        _validator = new CreateUserCommandValidator(CreateLocalizationMock());
    }

    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        mock.Setup(x => x["validation.email.required"]).Returns("Email is required");
        mock.Setup(x => x["validation.email.invalid"]).Returns("Email is not valid");
        mock.Setup(x => x.Get("validation.email.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Email cannot exceed {args[0]} characters");
        mock.Setup(x => x["validation.password.required"]).Returns("Password is required");
        mock.Setup(x => x.Get("validation.password.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Password must be at least {args[0]} characters");
        mock.Setup(x => x.Get("validation.password.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Password cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.displayName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Display name cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.firstName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"First name cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.lastName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Last name cannot exceed {args[0]} characters");

        return mock.Object;
    }

    #region Email Validation

    [Fact]
    public async Task Validate_WhenEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("", "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Fact]
    public async Task Validate_WhenEmailExceeds256Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 245) + "@example.com"; // 257 characters
        var command = new CreateUserCommand(longEmail, "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email cannot exceed 256 characters");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("invalid.com")]
    public async Task Validate_WhenEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new CreateUserCommand(email, "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is not valid");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@example.org")]
    [InlineData("admin@company.co.uk")]
    public async Task Validate_WhenEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new CreateUserCommand(email, "Password123", null, null, null, null);

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
        var command = new CreateUserCommand("user@example.com", "", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }

    [Theory]
    [InlineData("12345")] // 5 characters
    [InlineData("abc")] // 3 characters
    [InlineData("a")] // 1 character
    public async Task Validate_WhenPasswordTooShort_ShouldHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand("user@example.com", password, null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters");
    }

    [Fact]
    public async Task Validate_WhenPasswordTooLong_ShouldHaveError()
    {
        // Arrange
        var longPassword = new string('a', 101);
        var command = new CreateUserCommand("user@example.com", longPassword, null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("123456")] // 6 characters
    [InlineData("Password123")]
    [InlineData("SecurePassword!@#")]
    public async Task Validate_WhenPasswordIsValid_ShouldNotHaveError(string password)
    {
        // Arrange
        var command = new CreateUserCommand("user@example.com", password, null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region DisplayName Validation

    [Fact]
    public async Task Validate_WhenDisplayNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var longDisplayName = new string('a', 101);
        var command = new CreateUserCommand("user@example.com", "Password123", null, null, longDisplayName, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name cannot exceed 100 characters");
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var maxDisplayName = new string('a', 100);
        var command = new CreateUserCommand("user@example.com", "Password123", null, null, maxDisplayName, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("user@example.com", "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    #endregion

    #region FirstName Validation

    [Fact]
    public async Task Validate_WhenFirstNameExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var longFirstName = new string('a', 51);
        var command = new CreateUserCommand("user@example.com", "Password123", longFirstName, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name cannot exceed 50 characters");
    }

    [Fact]
    public async Task Validate_WhenFirstNameIs50Characters_ShouldNotHaveError()
    {
        // Arrange
        var maxFirstName = new string('a', 50);
        var command = new CreateUserCommand("user@example.com", "Password123", maxFirstName, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("user@example.com", "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region LastName Validation

    [Fact]
    public async Task Validate_WhenLastNameExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var longLastName = new string('a', 51);
        var command = new CreateUserCommand("user@example.com", "Password123", null, longLastName, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name cannot exceed 50 characters");
    }

    [Fact]
    public async Task Validate_WhenLastNameIs50Characters_ShouldNotHaveError()
    {
        // Arrange
        var maxLastName = new string('a', 50);
        var command = new CreateUserCommand("user@example.com", "Password123", null, maxLastName, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task Validate_WhenLastNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateUserCommand("user@example.com", "Password123", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateUserCommand(
            "user@example.com",
            "Password123",
            null,
            null,
            null,
            null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithAllFieldsIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateUserCommand(
            "user@example.com",
            "Password123",
            "John",
            "Doe",
            "John Doe",
            ["Admin", "User"],
            true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
