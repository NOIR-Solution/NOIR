namespace NOIR.Application.UnitTests.Validators;

/// <summary>
/// Unit tests for authentication command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class AuthValidatorsTests
{
    #region LoginCommandValidator Tests

    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new LoginCommand("user@example.com", "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyEmail_ShouldFail()
        {
            // Arrange
            var command = new LoginCommand("", "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void Validate_NullEmail_ShouldFail()
        {
            // Arrange
            var command = new LoginCommand(null!, "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@")]
        [InlineData("@nodomain.com")]
        public void Validate_InvalidEmailFormat_ShouldFail(string email)
        {
            // Arrange
            var command = new LoginCommand(email, "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name@domain.org")]
        [InlineData("user+tag@sub.domain.com")]
        public void Validate_ValidEmailFormats_ShouldPass(string email)
        {
            // Arrange
            var command = new LoginCommand(email, "password123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmptyPassword_ShouldFail()
        {
            // Arrange
            var command = new LoginCommand("user@example.com", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required.");
        }

        [Fact]
        public void Validate_NullPassword_ShouldFail()
        {
            // Arrange
            var command = new LoginCommand("user@example.com", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_MultipleErrors_ShouldReturnAll()
        {
            // Arrange
            var command = new LoginCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }

    #endregion

    #region RegisterCommandValidator Tests

    public class RegisterCommandValidatorTests
    {
        private readonly RegisterCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new RegisterCommand(
                "user@example.com",
                "password123",
                "John",
                "Doe");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MinimalValidCommand_ShouldPass()
        {
            // Arrange - Only required fields
            var command = new RegisterCommand(
                "user@example.com",
                "123456",
                null,
                null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyEmail_ShouldFail()
        {
            // Arrange
            var command = new RegisterCommand("", "password123", null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@")]
        public void Validate_InvalidEmailFormat_ShouldFail(string email)
        {
            // Arrange
            var command = new RegisterCommand(email, "password123", null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
        }

        [Fact]
        public void Validate_EmptyPassword_ShouldFail()
        {
            // Arrange
            var command = new RegisterCommand("user@example.com", "", null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required.");
        }

        [Theory]
        [InlineData("12345")] // 5 characters
        [InlineData("abcd")] // 4 characters
        [InlineData("a")] // 1 character
        public void Validate_PasswordTooShort_ShouldFail(string password)
        {
            // Arrange
            var command = new RegisterCommand("user@example.com", password, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must be at least 6 characters.");
        }

        [Theory]
        [InlineData("123456")] // Exactly 6 characters
        [InlineData("password")] // 8 characters
        [InlineData("VeryLongPasswordWithManyCharacters123!")] // Long password
        public void Validate_ValidPasswordLength_ShouldPass(string password)
        {
            // Arrange
            var command = new RegisterCommand("user@example.com", password, null, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_FirstNameTooLong_ShouldFail()
        {
            // Arrange
            var longFirstName = new string('a', 101); // 101 characters
            var command = new RegisterCommand("user@example.com", "password123", longFirstName, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName)
                .WithErrorMessage("First name cannot exceed 100 characters.");
        }

        [Fact]
        public void Validate_FirstNameAtMaxLength_ShouldPass()
        {
            // Arrange
            var maxFirstName = new string('a', 100); // 100 characters
            var command = new RegisterCommand("user@example.com", "password123", maxFirstName, null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Fact]
        public void Validate_LastNameTooLong_ShouldFail()
        {
            // Arrange
            var longLastName = new string('b', 101); // 101 characters
            var command = new RegisterCommand("user@example.com", "password123", null, longLastName);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LastName)
                .WithErrorMessage("Last name cannot exceed 100 characters.");
        }

        [Fact]
        public void Validate_LastNameAtMaxLength_ShouldPass()
        {
            // Arrange
            var maxLastName = new string('b', 100); // 100 characters
            var command = new RegisterCommand("user@example.com", "password123", null, maxLastName);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.LastName);
        }

        [Fact]
        public void Validate_AllFieldsInvalid_ShouldReturnAllErrors()
        {
            // Arrange
            var command = new RegisterCommand(
                "",
                "12345",
                new string('a', 101),
                new string('b', 101));

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
            result.ShouldHaveValidationErrorFor(x => x.LastName);
        }
    }

    #endregion

    #region RefreshTokenCommandValidator Tests

    public class RefreshTokenCommandValidatorTests
    {
        private readonly RefreshTokenCommandValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new RefreshTokenCommand(
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U",
                "dGhpcyBpcyBhIHZhbGlkIHJlZnJlc2ggdG9rZW4=");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyAccessToken_ShouldFail()
        {
            // Arrange
            var command = new RefreshTokenCommand("", "valid-refresh-token");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AccessToken);
        }

        [Fact]
        public void Validate_EmptyRefreshToken_ShouldFail()
        {
            // Arrange
            var command = new RefreshTokenCommand("valid-access-token", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
        }

        [Fact]
        public void Validate_BothTokensEmpty_ShouldFailBoth()
        {
            // Arrange
            var command = new RefreshTokenCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AccessToken);
            result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
        }
    }

    #endregion
}
