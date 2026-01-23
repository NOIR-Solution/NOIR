using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for UpdateUserProfileCommandValidator.
/// Tests validation rules for user profile updates.
/// </summary>
public class UpdateUserProfileCommandValidatorTests
{
    private readonly UpdateUserProfileCommandValidator _validator;
    private const int MaxNameLength = 50;
    private const int MaxDisplayNameLength = 100;
    private const int MaxPhoneLength = 20;

    public UpdateUserProfileCommandValidatorTests()
    {
        _validator = new UpdateUserProfileCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Name validations
        mock.Setup(x => x.Get("validation.firstName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"First name cannot exceed {args[0]} characters.");
        mock.Setup(x => x.Get("validation.lastName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Last name cannot exceed {args[0]} characters.");
        mock.Setup(x => x.Get("validation.displayName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Display name cannot exceed {args[0]} characters.");
        mock.Setup(x => x.Get("validation.phoneNumber.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Phone number cannot exceed {args[0]} characters.");

        return mock.Object;
    }

    #region FirstName Validation

    [Fact]
    public async Task Validate_WhenFirstNameExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: new string('A', MaxNameLength + 1),
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage($"First name cannot exceed {MaxNameLength} characters.");
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsExactlyMaxLength_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: new string('A', MaxNameLength),
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: null,
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("John")]
    [InlineData("A")]
    [InlineData("Mary Jane")]
    public async Task Validate_WhenFirstNameIsValid_ShouldNotHaveError(string firstName)
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: firstName,
            LastName: "Doe",
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region LastName Validation

    [Fact]
    public async Task Validate_WhenLastNameExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: new string('A', MaxNameLength + 1),
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage($"Last name cannot exceed {MaxNameLength} characters.");
    }

    [Fact]
    public async Task Validate_WhenLastNameIsExactlyMaxLength_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: new string('A', MaxNameLength),
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task Validate_WhenLastNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: null,
            DisplayName: "John Doe",
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Theory]
    [InlineData("Doe")]
    [InlineData("B")]
    [InlineData("Van Der Berg")]
    public async Task Validate_WhenLastNameIsValid_ShouldNotHaveError(string lastName)
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: lastName,
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    #endregion

    #region DisplayName Validation

    [Fact]
    public async Task Validate_WhenDisplayNameExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: new string('A', MaxDisplayNameLength + 1),
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.");
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsExactlyMaxLength_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: new string('A', MaxDisplayNameLength),
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: null,
            PhoneNumber: "1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData("JohnDoe")]
    [InlineData("Admin User")]
    [InlineData("J")]
    public async Task Validate_WhenDisplayNameIsValid_ShouldNotHaveError(string displayName)
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: null,
            LastName: null,
            DisplayName: displayName,
            PhoneNumber: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    #endregion

    #region PhoneNumber Validation

    [Fact]
    public async Task Validate_WhenPhoneNumberExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: new string('1', MaxPhoneLength + 1));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage($"Phone number cannot exceed {MaxPhoneLength} characters.");
    }

    [Fact]
    public async Task Validate_WhenPhoneNumberIsExactlyMaxLength_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: new string('1', MaxPhoneLength));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public async Task Validate_WhenPhoneNumberIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: "John Doe",
            PhoneNumber: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("555-0123")]
    [InlineData("+44 20 7946 0958")]
    public async Task Validate_WhenPhoneNumberIsValid_ShouldNotHaveError(string phone)
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: null,
            LastName: null,
            DisplayName: null,
            PhoneNumber: phone);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenAllFieldsAreNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: null,
            LastName: null,
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsFullyValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: "John",
            LastName: "Doe",
            DisplayName: "Johnny D",
            PhoneNumber: "+1234567890");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenMultipleFieldsExceedMaxLength_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new UpdateUserProfileCommand(
            FirstName: new string('A', MaxNameLength + 1),
            LastName: new string('B', MaxNameLength + 1),
            DisplayName: new string('C', MaxDisplayNameLength + 1),
            PhoneNumber: new string('1', MaxPhoneLength + 1));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion
}
