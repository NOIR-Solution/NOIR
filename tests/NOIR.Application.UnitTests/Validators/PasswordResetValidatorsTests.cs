namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;
using NOIR.Application.Features.Auth.Commands.PasswordReset.ResendPasswordResetOtp;
using NOIR.Application.Features.Auth.Commands.PasswordReset.ResetPassword;
using NOIR.Application.Features.Auth.Commands.PasswordReset.VerifyPasswordResetOtp;

/// <summary>
/// Unit tests for password reset command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class PasswordResetValidatorsTests
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
        mock.Setup(x => x["validation.minLength"]).Returns("Minimum length not met.");
        mock.Setup(x => x["validation.email"]).Returns("Invalid email format.");
        mock.Setup(x => x["validation.exactLength"]).Returns("OTP must be exactly 6 characters.");

        return mock.Object;
    }

    #region RequestPasswordResetCommandValidator Tests

    public class RequestPasswordResetCommandValidatorTests
    {
        private readonly RequestPasswordResetCommandValidator _validator;

        public RequestPasswordResetCommandValidatorTests()
        {
            _validator = new RequestPasswordResetCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new RequestPasswordResetCommand("user@example.com");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyEmail_ShouldFail()
        {
            // Arrange
            var command = new RequestPasswordResetCommand("");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullEmail_ShouldFail()
        {
            // Arrange
            var command = new RequestPasswordResetCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("missing@")]
        [InlineData("@nodomain.com")]
        [InlineData("invalid format")]
        public void Validate_InvalidEmailFormat_ShouldFail(string email)
        {
            // Arrange
            var command = new RequestPasswordResetCommand(email);

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
        [InlineData("test123@company.co.uk")]
        public void Validate_ValidEmailFormats_ShouldPass(string email)
        {
            // Arrange
            var command = new RequestPasswordResetCommand(email);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmailExceedsMaxLength_ShouldFail()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@test.com"; // 259 characters
            var command = new RequestPasswordResetCommand(longEmail);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Maximum length exceeded.");
        }

        [Fact]
        public void Validate_EmailAtMaxLength_ShouldPass()
        {
            // Arrange
            var maxEmail = new string('a', 240) + "@test.com"; // Just under 256 characters
            var command = new RequestPasswordResetCommand(maxEmail);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }
    }

    #endregion

    #region ResendPasswordResetOtpCommandValidator Tests

    public class ResendPasswordResetOtpCommandValidatorTests
    {
        private readonly ResendPasswordResetOtpCommandValidator _validator;

        public ResendPasswordResetOtpCommandValidatorTests()
        {
            _validator = new ResendPasswordResetOtpCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new ResendPasswordResetOtpCommand("valid-session-token-12345");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptySessionToken_ShouldFail()
        {
            // Arrange
            var command = new ResendPasswordResetOtpCommand("");

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
            var command = new ResendPasswordResetOtpCommand(null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Fact]
        public void Validate_WhitespaceSessionToken_ShouldFail()
        {
            // Arrange
            var command = new ResendPasswordResetOtpCommand("   ");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Theory]
        [InlineData("abc123")]
        [InlineData("session-token-with-dashes")]
        [InlineData("some.session.token.value")]
        [InlineData("uuid-like-token-123e4567")]
        public void Validate_ValidSessionTokenFormats_ShouldPass(string token)
        {
            // Arrange
            var command = new ResendPasswordResetOtpCommand(token);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion

    #region ResetPasswordCommandValidator Tests

    public class ResetPasswordCommandValidatorTests
    {
        private readonly ResetPasswordCommandValidator _validator;

        public ResetPasswordCommandValidatorTests()
        {
            _validator = new ResetPasswordCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new ResetPasswordCommand("valid-reset-token", "NewPassword123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyResetToken_ShouldFail()
        {
            // Arrange
            var command = new ResetPasswordCommand("", "NewPassword123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ResetToken)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullResetToken_ShouldFail()
        {
            // Arrange
            var command = new ResetPasswordCommand(null!, "NewPassword123!");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ResetToken);
        }

        [Fact]
        public void Validate_EmptyNewPassword_ShouldFail()
        {
            // Arrange
            var command = new ResetPasswordCommand("valid-reset-token", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("This field is required.");
        }

        [Fact]
        public void Validate_NullNewPassword_ShouldFail()
        {
            // Arrange
            var command = new ResetPasswordCommand("valid-reset-token", null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Theory]
        [InlineData("12345")]    // 5 chars - too short
        [InlineData("abcde")]    // 5 chars - too short
        [InlineData("Pass")]     // 4 chars - too short
        [InlineData("a")]        // 1 char - too short
        public void Validate_PasswordTooShort_ShouldFail(string password)
        {
            // Arrange
            var command = new ResetPasswordCommand("valid-reset-token", password);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Minimum length not met.");
        }

        [Theory]
        [InlineData("123456")]          // 6 chars - minimum
        [InlineData("abcdef")]          // 6 chars - minimum
        [InlineData("Password123!")]    // Strong password
        [InlineData("verylongpasswordthatisvalid")] // Long password
        public void Validate_ValidPasswordLengths_ShouldPass(string password)
        {
            // Arrange
            var command = new ResetPasswordCommand("valid-reset-token", password);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_MultipleErrors_ShouldReturnAll()
        {
            // Arrange
            var command = new ResetPasswordCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ResetToken);
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }
    }

    #endregion

    #region VerifyPasswordResetOtpCommandValidator Tests

    public class VerifyPasswordResetOtpCommandValidatorTests
    {
        private readonly VerifyPasswordResetOtpCommandValidator _validator;

        public VerifyPasswordResetOtpCommandValidatorTests()
        {
            _validator = new VerifyPasswordResetOtpCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("valid-session-token", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptySessionToken_ShouldFail()
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("", "123456");

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
            var command = new VerifyPasswordResetOtpCommand(null!, "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Fact]
        public void Validate_EmptyOtp_ShouldFail()
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("valid-session-token", "");

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
            var command = new VerifyPasswordResetOtpCommand("valid-session-token", null!);

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
            var command = new VerifyPasswordResetOtpCommand("valid-session-token", otp);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Otp)
                .WithErrorMessage("OTP must be exactly 6 characters.");
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("000000")]
        [InlineData("999999")]
        [InlineData("abcdef")]  // Letters are allowed by Length rule
        public void Validate_OtpCorrectLength_ShouldPass(string otp)
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("valid-session-token", otp);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Otp);
        }

        [Fact]
        public void Validate_WhitespaceSessionToken_ShouldFail()
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("   ", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
        }

        [Fact]
        public void Validate_MultipleErrors_ShouldReturnAll()
        {
            // Arrange
            var command = new VerifyPasswordResetOtpCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SessionToken);
            result.ShouldHaveValidationErrorFor(x => x.Otp);
        }
    }

    #endregion
}
