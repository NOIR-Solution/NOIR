namespace NOIR.Application.UnitTests.Validators;

/// <summary>
/// Unit tests for permission command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class PermissionValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Role ID validations
        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required");

        // Permission validations
        mock.Setup(x => x["validation.permissions.required"]).Returns("Permissions list is required");
        mock.Setup(x => x["validation.permissions.minOne"]).Returns("At least one permission must be specified");
        mock.Setup(x => x["validation.permissions.empty"]).Returns("Permission cannot be empty");
        mock.Setup(x => x.Get("validation.permissions.invalid", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Invalid permission: {args[0]}");

        return mock.Object;
    }

    #region AssignPermissionToRoleCommandValidator Tests

    public class AssignPermissionToRoleCommandValidatorTests
    {
        private readonly AssignPermissionToRoleCommandValidator _validator;

        public AssignPermissionToRoleCommandValidatorTests()
        {
            _validator = new AssignPermissionToRoleCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("role-id", [Permissions.UsersRead, Permissions.RolesRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_AllValidPermissions_ShouldPass()
        {
            // Arrange - Test with all available permissions
            var command = new AssignPermissionToRoleCommand("role-id", Permissions.All.ToList());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("", [Permissions.UsersRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleId)
                .WithErrorMessage("Role ID is required");
        }

        [Fact]
        public void Validate_NullRoleId_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand(null!, [Permissions.UsersRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleId);
        }

        [Fact]
        public void Validate_NullPermissions_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("role-id", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Permissions)
                .WithErrorMessage("Permissions list is required");
        }

        [Fact]
        public void Validate_EmptyPermissionInList_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("role-id", [Permissions.UsersRead, ""]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[1]")
                .WithErrorMessage("Permission cannot be empty");
        }

        [Fact]
        public void Validate_InvalidPermission_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("role-id", ["invalid.permission"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[0]");
        }

        [Fact]
        public void Validate_MixedValidAndInvalidPermissions_ShouldFail()
        {
            // Arrange
            var command = new AssignPermissionToRoleCommand("role-id", [Permissions.UsersRead, "invalid.permission"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor("Permissions[0]");
            result.ShouldHaveValidationErrorFor("Permissions[1]");
        }
    }

    #endregion

    #region RemovePermissionFromRoleCommandValidator Tests

    public class RemovePermissionFromRoleCommandValidatorTests
    {
        private readonly RemovePermissionFromRoleCommandValidator _validator;

        public RemovePermissionFromRoleCommandValidatorTests()
        {
            _validator = new RemovePermissionFromRoleCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new RemovePermissionFromRoleCommand("role-id", [Permissions.UsersRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var command = new RemovePermissionFromRoleCommand("", [Permissions.UsersRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleId)
                .WithErrorMessage("Role ID is required");
        }

        [Fact]
        public void Validate_NullPermissions_ShouldFail()
        {
            // Arrange
            var command = new RemovePermissionFromRoleCommand("role-id", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Permissions)
                .WithErrorMessage("Permissions list is required");
        }

        [Fact]
        public void Validate_EmptyPermissionInList_ShouldFail()
        {
            // Arrange
            var command = new RemovePermissionFromRoleCommand("role-id", [""]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[0]")
                .WithErrorMessage("Permission cannot be empty");
        }

        [Fact]
        public void Validate_InvalidPermission_ShouldFail()
        {
            // Arrange
            var command = new RemovePermissionFromRoleCommand("role-id", ["not.a.real.permission"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[0]");
        }
    }

    #endregion
}
