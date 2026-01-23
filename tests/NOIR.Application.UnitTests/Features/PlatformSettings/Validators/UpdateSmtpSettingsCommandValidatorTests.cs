namespace NOIR.Application.UnitTests.Features.PlatformSettings.Validators;

/// <summary>
/// Unit tests for UpdateSmtpSettingsCommandValidator.
/// Tests validation rules for updating platform SMTP settings.
/// </summary>
public class UpdateSmtpSettingsCommandValidatorTests
{
    private readonly UpdateSmtpSettingsCommandValidator _validator;

    public UpdateSmtpSettingsCommandValidatorTests()
    {
        _validator = new UpdateSmtpSettingsCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: "user@example.com",
            Password: "password123",
            FromEmail: "noreply@example.com",
            FromName: "My Application",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandHasNullOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My Application",
            UseSsl: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Host Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenHostIsEmptyOrWhitespace_ShouldHaveError(string? host)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: host!,
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host)
            .WithErrorMessage("SMTP host is required");
    }

    [Fact]
    public async Task Validate_WhenHostExceeds255Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: new string('a', 256),
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host)
            .WithErrorMessage("SMTP host cannot exceed 255 characters");
    }

    [Fact]
    public async Task Validate_WhenHostIs255Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: new string('a', 255),
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Host);
    }

    #endregion

    #region Port Validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public async Task Validate_WhenPortIsOutOfRange_ShouldHaveError(int port)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: port,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port)
            .WithErrorMessage("Port must be between 1 and 65535");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2525)]
    [InlineData(65535)]
    public async Task Validate_WhenPortIsValid_ShouldNotHaveError(int port)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: port,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    #endregion

    #region Username Validation

    [Fact]
    public async Task Validate_WhenUsernameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public async Task Validate_WhenUsernameExceeds255Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: new string('a', 256),
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username cannot exceed 255 characters");
    }

    #endregion

    #region Password Validation

    [Fact]
    public async Task Validate_WhenPasswordIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_WhenPasswordExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: new string('a', 501),
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password cannot exceed 500 characters");
    }

    #endregion

    #region FromEmail Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenFromEmailIsEmptyOrWhitespace_ShouldHaveError(string? email)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: email!,
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorMessage("From email is required");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WhenFromEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: email,
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorMessage("From email must be a valid email address");
    }

    [Fact]
    public async Task Validate_WhenFromEmailExceeds255Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 244) + "@example.com"; // 256 chars
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: longEmail,
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromEmail)
            .WithErrorMessage("From email cannot exceed 255 characters");
    }

    [Theory]
    [InlineData("noreply@example.com")]
    [InlineData("admin@test.org")]
    public async Task Validate_WhenFromEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: email,
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FromEmail);
    }

    #endregion

    #region FromName Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenFromNameIsEmptyOrWhitespace_ShouldHaveError(string? name)
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: name!,
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromName)
            .WithErrorMessage("From name is required");
    }

    [Fact]
    public async Task Validate_WhenFromNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: new string('a', 101),
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromName)
            .WithErrorMessage("From name cannot exceed 100 characters");
    }

    [Fact]
    public async Task Validate_WhenFromNameIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: new string('a', 100),
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FromName);
    }

    #endregion
}
