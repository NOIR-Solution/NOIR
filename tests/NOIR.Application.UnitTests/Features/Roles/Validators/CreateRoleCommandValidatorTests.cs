namespace NOIR.Application.UnitTests.Features.Roles.Validators;

/// <summary>
/// Unit tests for CreateRoleCommandValidator.
/// Tests validation rules for role creation including name pattern, length, and permissions.
/// </summary>
public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator;
    private const int MinRoleNameLength = 2;
    private const int MaxRoleNameLength = 50;

    public CreateRoleCommandValidatorTests()
    {
        _validator = new CreateRoleCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Role name validations
        mock.Setup(x => x["validation.roleName.required"]).Returns("Role name is required.");
        mock.Setup(x => x.Get("validation.roleName.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name must be at least {args[0]} characters.");
        mock.Setup(x => x.Get("validation.roleName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name cannot exceed {args[0]} characters.");
        mock.Setup(x => x["validation.roleName.pattern"]).Returns("Role name must start with a letter and contain only letters, numbers, hyphens, and underscores.");

        // Permission validations
        mock.Setup(x => x["validation.permissions.empty"]).Returns("Permission cannot be empty.");
        mock.Setup(x => x.Get("validation.permissions.invalid", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Permission '{args[0]}' is not a valid permission.");

        return mock.Object;
    }

    #region Name Validation - Empty/Null

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Role name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Name Validation - Minimum Length

    [Theory]
    [InlineData("A")]       // 1 char - too short
    public async Task Validate_WhenNameIsTooShort_ShouldHaveError(string name)
    {
        // Arrange
        var command = new CreateRoleCommand(name);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage($"Role name must be at least {MinRoleNameLength} characters.");
    }

    [Theory]
    [InlineData("Ab")]              // Exactly 2 chars - minimum valid
    [InlineData("Admin")]           // Standard role name
    [InlineData("SuperAdmin")]      // Longer role name
    public async Task Validate_WhenNameMeetsMinimumLength_ShouldNotHaveMinLengthError(string name)
    {
        // Arrange
        var command = new CreateRoleCommand(name);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Name Validation - Maximum Length

    [Fact]
    public async Task Validate_WhenNameExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var longName = "A" + new string('a', MaxRoleNameLength); // 51 chars
        var command = new CreateRoleCommand(longName);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage($"Role name cannot exceed {MaxRoleNameLength} characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIsExactlyMaxLength_ShouldNotHaveMaxLengthError()
    {
        // Arrange
        var maxName = "A" + new string('a', MaxRoleNameLength - 1); // Exactly 50 chars
        var command = new CreateRoleCommand(maxName);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Name Validation - Pattern (must start with letter, allow letters/numbers/hyphens/underscores)

    [Theory]
    [InlineData("1Admin")]          // Starts with number
    [InlineData("_admin")]          // Starts with underscore
    [InlineData("-admin")]          // Starts with hyphen
    [InlineData("admin role")]      // Contains space
    [InlineData("admin.role")]      // Contains dot
    [InlineData("admin@role")]      // Contains @
    [InlineData("admin!role")]      // Contains !
    [InlineData("admin#role")]      // Contains #
    public async Task Validate_WhenNameDoesNotMatchPattern_ShouldHaveError(string name)
    {
        // Arrange
        var command = new CreateRoleCommand(name);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Role name must start with a letter and contain only letters, numbers, hyphens, and underscores.");
    }

    [Theory]
    [InlineData("Admin")]               // Simple name
    [InlineData("SuperAdmin")]          // CamelCase
    [InlineData("admin")]               // Lowercase
    [InlineData("ADMIN")]               // Uppercase
    [InlineData("Admin123")]            // With numbers
    [InlineData("Admin-User")]          // With hyphen
    [InlineData("Admin_User")]          // With underscore
    [InlineData("a1")]                  // Minimum valid with number
    [InlineData("Role-With-Hyphens")]   // Multiple hyphens
    [InlineData("Role_With_Under")]     // Multiple underscores
    public async Task Validate_WhenNameMatchesPattern_ShouldNotHavePatternError(string name)
    {
        // Arrange
        var command = new CreateRoleCommand(name);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Permissions Validation

    [Fact]
    public async Task Validate_WhenPermissionsContainsEmptyString_ShouldHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: new List<string> { "" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Permissions[0]")
            .WithErrorMessage("Permission cannot be empty.");
    }

    [Fact]
    public async Task Validate_WhenPermissionsContainsInvalidPermission_ShouldHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: new List<string> { "invalid:permission" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Permissions[0]")
            .WithErrorMessage("Permission 'invalid:permission' is not a valid permission.");
    }

    [Fact]
    public async Task Validate_WhenPermissionsContainsValidPermissions_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: new List<string>
        {
            Domain.Common.Permissions.UsersRead,
            Domain.Common.Permissions.RolesRead
        });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenPermissionsIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenPermissionsIsEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: new List<string>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenPermissionsContainsMixOfValidAndInvalid_ShouldHaveErrorForInvalid()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin", Permissions: new List<string>
        {
            Domain.Common.Permissions.UsersRead,
            "fake:permission",
            Domain.Common.Permissions.RolesRead
        });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Permissions[1]")
            .WithErrorMessage("Permission 'fake:permission' is not a valid permission.");
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateRoleCommand("Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithAllOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateRoleCommand(
            "Admin",
            Description: "Administrator role",
            ParentRoleId: "parent-role-id",
            TenantId: Guid.NewGuid(),
            SortOrder: 1,
            IconName: "shield",
            Color: "#FF0000",
            Permissions: new List<string> { Domain.Common.Permissions.UsersRead });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenNameIsEmptyAndPermissionsContainInvalid_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new CreateRoleCommand("", Permissions: new List<string> { "invalid:perm" });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor("Permissions[0]");
    }

    #endregion
}
