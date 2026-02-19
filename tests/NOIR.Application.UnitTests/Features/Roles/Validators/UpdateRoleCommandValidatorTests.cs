namespace NOIR.Application.UnitTests.Features.Roles.Validators;

/// <summary>
/// Unit tests for UpdateRoleCommandValidator.
/// Tests validation rules for role update including RoleId, name pattern, and length.
/// </summary>
public class UpdateRoleCommandValidatorTests
{
    private readonly UpdateRoleCommandValidator _validator;
    private const int MinRoleNameLength = 2;
    private const int MaxRoleNameLength = 50;

    public UpdateRoleCommandValidatorTests()
    {
        _validator = new UpdateRoleCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // RoleId validation
        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required.");

        // Role name validations
        mock.Setup(x => x["validation.roleName.required"]).Returns("Role name is required.");
        mock.Setup(x => x.Get("validation.roleName.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name must be at least {args[0]} characters.");
        mock.Setup(x => x.Get("validation.roleName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name cannot exceed {args[0]} characters.");
        mock.Setup(x => x["validation.roleName.pattern"]).Returns("Role name must start with a letter and contain only letters, numbers, hyphens, and underscores.");

        return mock.Object;
    }

    #region RoleId Validation

    [Fact]
    public async Task Validate_WhenRoleIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRoleCommand("", "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId)
            .WithErrorMessage("Role ID is required.");
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRoleCommand(null!, "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRoleCommand("   ", "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Theory]
    [InlineData("role-123")]
    [InlineData("abc")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    public async Task Validate_WhenRoleIdIsValid_ShouldNotHaveError(string roleId)
    {
        // Arrange
        var command = new UpdateRoleCommand(roleId, "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    #endregion

    #region Name Validation - Empty/Null

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRoleCommand("role-123", "");

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
        var command = new UpdateRoleCommand("role-123", null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRoleCommand("role-123", "   ");

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
        var command = new UpdateRoleCommand("role-123", name);

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
        var command = new UpdateRoleCommand("role-123", name);

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
        var command = new UpdateRoleCommand("role-123", longName);

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
        var command = new UpdateRoleCommand("role-123", maxName);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Name Validation - Pattern

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
        var command = new UpdateRoleCommand("role-123", name);

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
        var command = new UpdateRoleCommand("role-123", name);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateRoleCommand("role-123", "Admin");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithAllOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateRoleCommand(
            "role-123",
            "Admin",
            Description: "Updated administrator role",
            ParentRoleId: "parent-role-id",
            SortOrder: 2,
            IconName: "shield-check",
            Color: "#00FF00");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenRoleIdAndNameAreEmpty_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new UpdateRoleCommand("", "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenRoleIdIsEmptyAndNameIsTooShort_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new UpdateRoleCommand("", "A");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    #endregion
}
