namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

using NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;

/// <summary>
/// Unit tests for UpdateTenantSmtpSettingsCommandValidator.
/// Tests validation rules for updating tenant SMTP settings.
/// </summary>
public class UpdateTenantSmtpSettingsCommandValidatorTests
{
    private readonly UpdateTenantSmtpSettingsCommandValidator _validator;

    public UpdateTenantSmtpSettingsCommandValidatorTests()
    {
        _validator = new UpdateTenantSmtpSettingsCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: "user@example.com",
            Password: "password123",
            FromEmail: "noreply@example.com",
            FromName: "My Tenant App",
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
        var command = new UpdateTenantSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 25,
            Username: null,
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My Tenant App",
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
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("SMTP host is required.");
    }

    [Fact]
    public async Task Validate_WhenHostExceeds255Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("SMTP host must not exceed 255 characters.");
    }

    [Fact]
    public async Task Validate_WhenHostIs255Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("Port must be between 1 and 65535.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(65535)]
    public async Task Validate_WhenPortIsValid_ShouldNotHaveError(int port)
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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

    #region FromEmail Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenFromEmailIsEmptyOrWhitespace_ShouldHaveError(string? email)
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("From email is required.");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WhenFromEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("From email must be a valid email address.");
    }

    [Fact]
    public async Task Validate_WhenFromEmailExceeds255Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 244) + "@example.com"; // 256 chars
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("From email must not exceed 255 characters.");
    }

    [Theory]
    [InlineData("noreply@example.com")]
    [InlineData("admin@test.org")]
    public async Task Validate_WhenFromEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("From name is required.");
    }

    [Fact]
    public async Task Validate_WhenFromNameExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("From name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenFromNameIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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

    #region Username Validation

    [Fact]
    public async Task Validate_WhenUsernameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
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
        var command = new UpdateTenantSmtpSettingsCommand(
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
            .WithErrorMessage("Username must not exceed 255 characters.");
    }

    [Fact]
    public async Task Validate_WhenUsernameIs255Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantSmtpSettingsCommand(
            Host: "smtp.example.com",
            Port: 587,
            Username: new string('a', 255),
            Password: null,
            FromEmail: "noreply@example.com",
            FromName: "My App",
            UseSsl: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion
}
