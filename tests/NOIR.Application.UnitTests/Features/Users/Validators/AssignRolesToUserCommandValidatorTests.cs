namespace NOIR.Application.UnitTests.Features.Users.Validators;

/// <summary>
/// Unit tests for AssignRolesToUserCommandValidator.
/// Tests all validation rules for assigning roles to a user.
/// </summary>
public class AssignRolesToUserCommandValidatorTests
{
    private readonly AssignRolesToUserCommandValidator _validator;

    public AssignRolesToUserCommandValidatorTests()
    {
        _validator = new AssignRolesToUserCommandValidator(CreateLocalizationMock());
    }

    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x["validation.userId.required"]).Returns("User ID is required");
        mock.Setup(x => x["validation.rolesList.required"]).Returns("Roles list is required");
        mock.Setup(x => x["validation.roleName.empty"]).Returns("Role name cannot be empty");
        return mock.Object;
    }

    #region UserId Validation

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("", ["Admin"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public async Task Validate_WhenUserIdIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand(null!, ["Admin"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Validate_WhenUserIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region RoleNames Validation

    [Fact]
    public async Task Validate_WhenRoleNamesIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RoleNames)
            .WithErrorMessage("Roles list is required");
    }

    [Fact]
    public async Task Validate_WhenEmptyRoleNameInList_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin", ""]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("RoleNames[1]")
            .WithErrorMessage("Role name cannot be empty");
    }

    [Fact]
    public async Task Validate_WhenRoleNamesIsEmptyList_ShouldNotHaveError()
    {
        // Arrange - Empty list is valid (removes all roles)
        var command = new AssignRolesToUserCommand("user-id", []);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenRoleNamesHasValidRoles_ShouldNotHaveError()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin", "User"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RoleNames);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin", "User"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithSingleRole_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin"]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandWithEmail_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AssignRolesToUserCommand("user-id", ["Admin"], UserEmail: "user@example.com");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
