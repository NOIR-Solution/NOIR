namespace NOIR.Application.UnitTests.Validators;

/// <summary>
/// Unit tests for authentication command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class AuthValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Email validations
        mock.Setup(x => x["validation.email.required"]).Returns("Email is required.");
        mock.Setup(x => x["validation.email.invalid"]).Returns("Invalid email format.");

        // Password validations
        mock.Setup(x => x["validation.password.required"]).Returns("Password is required.");
        mock.Setup(x => x.Get("validation.password.tooShort", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Password must be at least {args[0]} characters.");

        // Name validations
        mock.Setup(x => x.Get("validation.firstName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"First name cannot exceed {args[0]} characters.");
        mock.Setup(x => x.Get("validation.lastName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Last name cannot exceed {args[0]} characters.");

        // Token validations
        mock.Setup(x => x["validation.accessToken.required"]).Returns("Access token is required.");
        mock.Setup(x => x["validation.refreshToken.required"]).Returns("Refresh token is required when not using cookies.");

        return mock.Object;
    }

    #region LoginCommandValidator Tests

    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator;

        public LoginCommandValidatorTests()
        {
            _validator = new LoginCommandValidator(CreateLocalizationMock());
        }

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

    #region RefreshTokenCommandValidator Tests

    public class RefreshTokenCommandValidatorTests
    {
        private readonly RefreshTokenCommandValidator _validator;

        public RefreshTokenCommandValidatorTests()
        {
            _validator = new RefreshTokenCommandValidator(CreateLocalizationMock());
        }

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
