namespace NOIR.Application.UnitTests.Features.Users.Validators;

/// <summary>
/// Unit tests for UpdateUserCommandValidator.
/// Tests all validation rules for updating a user.
/// </summary>
public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator;

    public UpdateUserCommandValidatorTests()
    {
        _validator = new UpdateUserCommandValidator(CreateLocalizationMock());
    }

    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        mock.Setup(x => x["validation.userId.required"]).Returns("User ID is required");
        mock.Setup(x => x.Get("validation.displayName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Display name cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.firstName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"First name cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.lastName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Last name cannot exceed {args[0]} characters");

        return mock.Object;
    }

    #region TargetUserId Validation

    [Fact]
    public async Task Validate_WhenTargetUserIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateUserCommand("", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public async Task Validate_WhenTargetUserIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserCommand("user-id", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TargetUserId);
    }

    #endregion

    #region DisplayName Validation

    [Fact]
    public async Task Validate_WhenDisplayNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var longDisplayName = new string('a', 101);
        var command = new UpdateUserCommand("user-id", longDisplayName, null, null, null);

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
        var command = new UpdateUserCommand("user-id", maxDisplayName, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserCommand("user-id", null, null, null, null);

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
        var command = new UpdateUserCommand("user-id", null, longFirstName, null, null);

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
        var command = new UpdateUserCommand("user-id", null, maxFirstName, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task Validate_WhenFirstNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserCommand("user-id", null, null, null, null);

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
        var command = new UpdateUserCommand("user-id", null, null, longLastName, null);

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
        var command = new UpdateUserCommand("user-id", null, null, maxLastName, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task Validate_WhenLastNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateUserCommand("user-id", null, null, null, null);

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
        var command = new UpdateUserCommand("user-id", "Display Name", "John", "Doe", true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange - Only required field
        var command = new UpdateUserCommand("user-id", null, null, null, null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
