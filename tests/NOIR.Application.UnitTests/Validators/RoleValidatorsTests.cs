namespace NOIR.Application.UnitTests.Validators;

/// <summary>
/// Unit tests for role command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class RoleValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Role ID validations
        mock.Setup(x => x["validation.roleId.required"]).Returns("Role ID is required");

        // Role name validations
        mock.Setup(x => x["validation.roleName.required"]).Returns("Role name is required");
        mock.Setup(x => x["validation.roleName.pattern"]).Returns("Role name must start with a letter and contain only letters, numbers, underscores, and hyphens");
        mock.Setup(x => x.Get("validation.roleName.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name must be at least {args[0]} characters");
        mock.Setup(x => x.Get("validation.roleName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Role name cannot exceed {args[0]} characters");

        // Permission validations
        mock.Setup(x => x["validation.permissions.empty"]).Returns("Permission cannot be empty");
        mock.Setup(x => x.Get("validation.permissions.invalid", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Invalid permission: {args[0]}");

        return mock.Object;
    }

    #region CreateRoleCommandValidator Tests

    public class CreateRoleCommandValidatorTests
    {
        private readonly CreateRoleCommandValidator _validator;

        public CreateRoleCommandValidatorTests()
        {
            _validator = new CreateRoleCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateRoleCommand("TestRole", []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithPermissions_ShouldPass()
        {
            // Arrange
            var command = new CreateRoleCommand("TestRole", [Permissions.UsersRead, Permissions.RolesRead]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyName_ShouldFail()
        {
            // Arrange
            var command = new CreateRoleCommand("", []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name is required");
        }

        [Theory]
        [InlineData("A")] // 1 character
        public void Validate_NameTooShort_ShouldFail(string name)
        {
            // Arrange
            var command = new CreateRoleCommand(name, []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name must be at least 2 characters");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 51);
            var command = new CreateRoleCommand(longName, []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name cannot exceed 50 characters");
        }

        [Theory]
        [InlineData("123Role")] // Starts with number
        [InlineData("_Role")] // Starts with underscore
        [InlineData("-Role")] // Starts with hyphen
        [InlineData("Role Name")] // Contains space
        [InlineData("Role@Name")] // Contains invalid character
        public void Validate_InvalidNameFormat_ShouldFail(string name)
        {
            // Arrange
            var command = new CreateRoleCommand(name, []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name must start with a letter and contain only letters, numbers, underscores, and hyphens");
        }

        [Theory]
        [InlineData("Role")]
        [InlineData("Role123")]
        [InlineData("Role_Name")]
        [InlineData("Role-Name")]
        [InlineData("TestRole")]
        public void Validate_ValidNameFormats_ShouldPass(string name)
        {
            // Arrange
            var command = new CreateRoleCommand(name, []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_InvalidPermission_ShouldFail()
        {
            // Arrange
            var command = new CreateRoleCommand("TestRole", ["invalid.permission"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[0]");
        }

        [Fact]
        public void Validate_EmptyPermissionInList_ShouldFail()
        {
            // Arrange
            var command = new CreateRoleCommand("TestRole", [""]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Permissions[0]")
                .WithErrorMessage("Permission cannot be empty");
        }
    }

    #endregion

    #region UpdateRoleCommandValidator Tests

    public class UpdateRoleCommandValidatorTests
    {
        private readonly UpdateRoleCommandValidator _validator;

        public UpdateRoleCommandValidatorTests()
        {
            _validator = new UpdateRoleCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateRoleCommand("role-id", "UpdatedRole");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var command = new UpdateRoleCommand("", "UpdatedRole");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleId)
                .WithErrorMessage("Role ID is required");
        }

        [Fact]
        public void Validate_EmptyName_ShouldFail()
        {
            // Arrange
            var command = new UpdateRoleCommand("role-id", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name is required");
        }

        [Fact]
        public void Validate_NameTooShort_ShouldFail()
        {
            // Arrange
            var command = new UpdateRoleCommand("role-id", "A");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name must be at least 2 characters");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 51);
            var command = new UpdateRoleCommand("role-id", longName);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Role name cannot exceed 50 characters");
        }

        [Theory]
        [InlineData("123Role")]
        [InlineData("Role Name")]
        public void Validate_InvalidNameFormat_ShouldFail(string name)
        {
            // Arrange
            var command = new UpdateRoleCommand("role-id", name);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }
    }

    #endregion

    #region DeleteRoleCommandValidator Tests

    public class DeleteRoleCommandValidatorTests
    {
        private readonly DeleteRoleCommandValidator _validator;

        public DeleteRoleCommandValidatorTests()
        {
            _validator = new DeleteRoleCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeleteRoleCommand("role-id");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyRoleId_ShouldFail()
        {
            // Arrange
            var command = new DeleteRoleCommand("");

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
            var command = new DeleteRoleCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleId);
        }
    }

    #endregion
}
