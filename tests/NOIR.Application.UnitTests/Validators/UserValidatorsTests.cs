namespace NOIR.Application.UnitTests.Validators;

/// <summary>
/// Unit tests for user management command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class UserValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // User ID validations
        mock.Setup(x => x["validation.userId.required"]).Returns("User ID is required");

        // Display name validations
        mock.Setup(x => x.Get("validation.displayName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Display name cannot exceed {args[0]} characters");

        // Name validations
        mock.Setup(x => x.Get("validation.firstName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"First name cannot exceed {args[0]} characters");
        mock.Setup(x => x.Get("validation.lastName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Last name cannot exceed {args[0]} characters");

        // Roles validations
        mock.Setup(x => x["validation.rolesList.required"]).Returns("Roles list is required");
        mock.Setup(x => x["validation.roleName.empty"]).Returns("Role name cannot be empty");

        return mock.Object;
    }

    #region UpdateUserCommandValidator Tests

    public class UpdateUserCommandValidatorTests
    {
        private readonly UpdateUserCommandValidator _validator;

        public UpdateUserCommandValidatorTests()
        {
            _validator = new UpdateUserCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateUserCommand("user-id", "Display Name", "John", "Doe", true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MinimalCommand_ShouldPass()
        {
            // Arrange - Only required field
            var command = new UpdateUserCommand("user-id", null, null, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyUserId_ShouldFail()
        {
            // Arrange
            var command = new UpdateUserCommand("", null, null, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Fact]
        public void Validate_DisplayNameTooLong_ShouldFail()
        {
            // Arrange
            var longDisplayName = new string('a', 101);
            var command = new UpdateUserCommand("user-id", longDisplayName, null, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DisplayName)
                .WithErrorMessage("Display name cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_DisplayNameAtMaxLength_ShouldPass()
        {
            // Arrange
            var maxDisplayName = new string('a', 100);
            var command = new UpdateUserCommand("user-id", maxDisplayName, null, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
        }

        [Fact]
        public void Validate_FirstNameTooLong_ShouldFail()
        {
            // Arrange
            var longFirstName = new string('a', 51);
            var command = new UpdateUserCommand("user-id", null, longFirstName, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                .WithErrorMessage("First name cannot exceed 50 characters");
        }

        [Fact]
        public void Validate_LastNameTooLong_ShouldFail()
        {
            // Arrange
            var longLastName = new string('a', 51);
            var command = new UpdateUserCommand("user-id", null, null, longLastName, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                .WithErrorMessage("Last name cannot exceed 50 characters");
        }
    }

    #endregion

    #region DeleteUserCommandValidator Tests

    public class DeleteUserCommandValidatorTests
    {
        private readonly DeleteUserCommandValidator _validator;

        public DeleteUserCommandValidatorTests()
        {
            _validator = new DeleteUserCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeleteUserCommand("user-id");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyUserId_ShouldFail()
        {
            // Arrange
            var command = new DeleteUserCommand("");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Fact]
        public void Validate_NullUserId_ShouldFail()
        {
            // Arrange
            var command = new DeleteUserCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }
    }

    #endregion

    #region AssignRolesToUserCommandValidator Tests

    public class AssignRolesToUserCommandValidatorTests
    {
        private readonly AssignRolesToUserCommandValidator _validator;

        public AssignRolesToUserCommandValidatorTests()
        {
            _validator = new AssignRolesToUserCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new AssignRolesToUserCommand("user-id", ["Admin", "User"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyRolesList_ShouldPass()
        {
            // Arrange - Empty list is valid (removes all roles)
            var command = new AssignRolesToUserCommand("user-id", []);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyUserId_ShouldFail()
        {
            // Arrange
            var command = new AssignRolesToUserCommand("", ["Admin"]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("User ID is required");
        }

        [Fact]
        public void Validate_NullRoleNames_ShouldFail()
        {
            // Arrange
            var command = new AssignRolesToUserCommand("user-id", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RoleNames)
                .WithErrorMessage("Roles list is required");
        }

        [Fact]
        public void Validate_EmptyRoleNameInList_ShouldFail()
        {
            // Arrange
            var command = new AssignRolesToUserCommand("user-id", ["Admin", ""]);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("RoleNames[1]")
                .WithErrorMessage("Role name cannot be empty");
        }
    }

    #endregion
}
