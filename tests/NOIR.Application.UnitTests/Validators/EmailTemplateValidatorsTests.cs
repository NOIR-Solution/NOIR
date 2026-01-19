namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.EmailTemplates.Commands.SendTestEmail;
using NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Unit tests for email template command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class EmailTemplateValidatorsTests
{
    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        return mock.Object;
    }

    #region SendTestEmailCommandValidator Tests

    public class SendTestEmailCommandValidatorTests
    {
        private readonly SendTestEmailCommandValidator _validator;

        public SendTestEmailCommandValidatorTests()
        {
            _validator = new SendTestEmailCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: "test@example.com",
                SampleData: new Dictionary<string, string>
                {
                    { "Name", "John Doe" },
                    { "Subject", "Test Subject" }
                });

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyTemplateId_ShouldFail()
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.Empty,
                RecipientEmail: "test@example.com",
                SampleData: new Dictionary<string, string>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TemplateId)
                .WithErrorMessage("Template ID is required.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullRecipientEmail_ShouldFail(string? email)
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: email!,
                SampleData: new Dictionary<string, string>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
                .WithErrorMessage("Recipient email is required.");
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("invalid@")]
        [InlineData("@example.com")]
        [InlineData("invalid email")]
        public void Validate_InvalidEmailFormat_ShouldFail(string email)
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: email,
                SampleData: new Dictionary<string, string>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
                .WithErrorMessage("Invalid email address format.");
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user.name@example.com")]
        [InlineData("user+tag@example.org")]
        [InlineData("admin@subdomain.example.co.uk")]
        public void Validate_ValidEmailFormats_ShouldPass(string email)
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: email,
                SampleData: new Dictionary<string, string>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.RecipientEmail);
        }

        [Fact]
        public void Validate_NullSampleData_ShouldFail()
        {
            // Arrange
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: "test@example.com",
                SampleData: null!);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SampleData)
                .WithErrorMessage("Sample data is required.");
        }

        [Fact]
        public void Validate_EmptySampleData_ShouldPass()
        {
            // Arrange - Empty dictionary is valid, just null is not
            var command = new SendTestEmailCommand(
                TemplateId: Guid.NewGuid(),
                RecipientEmail: "test@example.com",
                SampleData: new Dictionary<string, string>());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SampleData);
        }
    }

    #endregion

    #region UpdateEmailTemplateCommandValidator Tests

    public class UpdateEmailTemplateCommandValidatorTests
    {
        private readonly UpdateEmailTemplateCommandValidator _validator;

        public UpdateEmailTemplateCommandValidatorTests()
        {
            _validator = new UpdateEmailTemplateCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: "<html><body>Hello {{Name}}</body></html>",
                PlainTextBody: "Hello {{Name}}",
                Description: "Test email template description");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithMinimalFields_ShouldPass()
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.Empty,
                Subject: "Test Subject",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Template ID is required.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullSubject_ShouldFail(string? subject)
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: subject!,
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Subject)
                .WithErrorMessage("Subject is required.");
        }

        [Fact]
        public void Validate_SubjectTooLong_ShouldFail()
        {
            // Arrange
            var longSubject = new string('a', 501);
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: longSubject,
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Subject)
                .WithErrorMessage("Subject cannot exceed 500 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullHtmlBody_ShouldFail(string? htmlBody)
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: htmlBody!,
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.HtmlBody)
                .WithErrorMessage("HTML body is required.");
        }

        [Fact]
        public void Validate_DescriptionTooLong_ShouldFail()
        {
            // Arrange
            var longDescription = new string('a', 1001);
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: longDescription);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description cannot exceed 1000 characters.");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(500)]
        [InlineData(1000)]
        public void Validate_ValidDescriptionLengths_ShouldPass(int length)
        {
            // Arrange
            var description = new string('a', length);
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: description);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_NullDescription_ShouldPass()
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_ValidSubjectWithSpecialCharacters_ShouldPass()
        {
            // Arrange
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Welcome {{UserName}} to {{CompanyName}}!",
                HtmlBody: "<html><body>Content</body></html>",
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Subject);
        }

        [Fact]
        public void Validate_LargeHtmlBody_ShouldPass()
        {
            // Arrange - Large HTML body should be valid (no max length validation on HTML body)
            var largeHtmlBody = "<html><body>" + new string('a', 100000) + "</body></html>";
            var command = new UpdateEmailTemplateCommand(
                Id: Guid.NewGuid(),
                Subject: "Test Subject",
                HtmlBody: largeHtmlBody,
                PlainTextBody: null,
                Description: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.HtmlBody);
        }
    }

    #endregion
}
