namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.Auth.Commands.ChangeEmail;

/// <summary>
/// Unit tests for email change command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class EmailChangeValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");
        mock.Setup(x => x["validation.maxLength"]).Returns("Maximum length exceeded.");
        mock.Setup(x => x["validation.email"]).Returns("Invalid email format.");
        mock.Setup(x => x["validation.otp.length"]).Returns("OTP must be 6 digits.");

        return mock.Object;
    }

    #region RequestEmailChangeCommandValidator Tests

    public class RequestEmailChangeCommandValidatorTests
    {
        private readonly RequestEmailChangeCommandValidator _validator;

        public RequestEmailChangeCommandValidatorTests()
        {
            _validator = new RequestEmailChangeCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new RequestEmailChangeCommand("newemail@example.com");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyNewEmail_ShouldFail()
        {
            // Arrange
            var command = new RequestEmailChangeCommand("");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewEmail)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullNewEmail_ShouldFail()
        {
            // Arrange
            var command = new RequestEmailChangeCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewEmail);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@")]
        [InlineData("@nodomain.com")]
        [InlineData("double@@at.com")]
        public void Validate_InvalidEmailFormat_ShouldFail(string email)
        {
            // Arrange
            var command = new RequestEmailChangeCommand(email);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewEmail)
                .WithErrorMessage("Invalid email format.");
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("user.name@domain.org")]
        [InlineData("user+tag@sub.domain.com")]
        [InlineData("test123@company.co.uk")]
        public void Validate_ValidEmailFormats_ShouldPass(string email)
        {
            // Arrange
            var command = new RequestEmailChangeCommand(email);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
        }

        [Fact]
        public void Validate_EmailExceedsMaxLength_ShouldFail()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@test.com"; // 259 characters
            var command = new RequestEmailChangeCommand(longEmail);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewEmail)
                .WithErrorMessage("Maximum length exceeded.");
        }

        [Fact]
        public void Validate_EmailAtMaxLength_ShouldPass()
        {
            // Arrange
            var maxEmail = new string('a', 240) + "@test.com"; // Just under 256 characters
            var command = new RequestEmailChangeCommand(maxEmail);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewEmail);
        }
    }

    #endregion

    #region ResendEmailChangeOtpCommandValidator Tests

    public class ResendEmailChangeOtpCommandValidatorTests
    {
        private readonly ResendEmailChangeOtpCommandValidator _validator;

        public ResendEmailChangeOtpCommandValidatorTests()
        {
            _validator = new ResendEmailChangeOtpCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new ResendEmailChangeOtpCommand("valid-session-token-12345");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptySessionToken_ShouldFail()
        {
            // Arrange
            var command = new ResendEmailChangeOtpCommand("");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullSessionToken_ShouldFail()
        {
            // Arrange
            var command = new ResendEmailChangeOtpCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Fact]
        public void Validate_WhitespaceSessionToken_ShouldFail()
        {
            // Arrange
            var command = new ResendEmailChangeOtpCommand("   ");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("a-b-c-d-e")]
        [InlineData("some.session.token.value")]
        public void Validate_ValidSessionTokenFormats_ShouldPass(string token)
        {
            // Arrange
            var command = new ResendEmailChangeOtpCommand(token);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion

    #region VerifyEmailChangeCommandValidator Tests

    public class VerifyEmailChangeCommandValidatorTests
    {
        private readonly VerifyEmailChangeCommandValidator _validator;

        public VerifyEmailChangeCommandValidatorTests()
        {
            _validator = new VerifyEmailChangeCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("valid-session-token", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptySessionToken_ShouldFail()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullSessionToken_ShouldFail()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand(null!, "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Fact]
        public void Validate_EmptyOtp_ShouldFail()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("valid-session-token", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Otp)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullOtp_ShouldFail()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("valid-session-token", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Otp);
        }

        [Theory]
        [InlineData("12345")]     // 5 digits - too short
        [InlineData("1234567")]   // 7 digits - too long
        [InlineData("1234")]      // 4 digits - too short
        [InlineData("12345678")]  // 8 digits - too long
        public void Validate_OtpWrongLength_ShouldFail(string otp)
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("valid-session-token", otp);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Otp)
                .WithErrorMessage("OTP must be 6 digits.");
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("000000")]
        [InlineData("999999")]
        [InlineData("abcdef")]  // Letters are allowed by Length rule
        public void Validate_OtpCorrectLength_ShouldPass(string otp)
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("valid-session-token", otp);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Otp);
        }

        [Fact]
        public void Validate_MultipleErrors_ShouldReturnAll()
        {
            // Arrange
            var command = new VerifyEmailChangeCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
            result.ShouldHaveValidationErrorFor(x => x.Otp);
        }
    }

    #endregion
}
