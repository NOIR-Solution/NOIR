namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

/// <summary>
/// Unit tests for UpdateBrandingSettingsCommandValidator.
/// Tests validation rules for updating branding settings.
/// </summary>
public class UpdateBrandingSettingsCommandValidatorTests
{
    private readonly UpdateBrandingSettingsCommandValidator _validator;

    public UpdateBrandingSettingsCommandValidatorTests()
    {
        _validator = new UpdateBrandingSettingsCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: "https://example.com/logo.png",
            FaviconUrl: "https://example.com/favicon.ico",
            PrimaryColor: "#FF5733",
            SecondaryColor: "#333FFF",
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenAllFieldsAreNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenColorsAreEmpty_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: "",
            SecondaryColor: "",
            DarkModeDefault: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region LogoUrl Validation

    [Fact]
    public async Task Validate_WhenLogoUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: new string('a', 2001),
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LogoUrl)
            .WithErrorMessage("Logo URL must not exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenLogoUrlIs2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: new string('a', 2000),
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LogoUrl);
    }

    #endregion

    #region FaviconUrl Validation

    [Fact]
    public async Task Validate_WhenFaviconUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: new string('a', 2001),
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FaviconUrl)
            .WithErrorMessage("Favicon URL must not exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenFaviconUrlIs2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: new string('a', 2000),
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FaviconUrl);
    }

    #endregion

    #region PrimaryColor Validation

    [Fact]
    public async Task Validate_WhenPrimaryColorExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: new string('a', 51),
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PrimaryColor)
            .WithErrorMessage("Primary color must not exceed 50 characters.");
    }

    [Theory]
    [InlineData("invalid-color")]
    [InlineData("FF5733")]
    [InlineData("#GG5733")]
    [InlineData("#FFFF")]
    [InlineData("#FF57330")]
    public async Task Validate_WhenPrimaryColorHasInvalidFormat_ShouldHaveError(string color)
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: color,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PrimaryColor)
            .WithErrorMessage("Primary color must be a valid hex color (e.g., #FF5733).");
    }

    [Theory]
    [InlineData("#FF5733")]
    [InlineData("#abc123")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#FFF")]
    [InlineData("#abc")]
    public async Task Validate_WhenPrimaryColorIsValidHex_ShouldNotHaveError(string color)
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: color,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PrimaryColor);
    }

    [Fact]
    public async Task Validate_WhenPrimaryColorIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: null,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PrimaryColor);
    }

    #endregion

    #region SecondaryColor Validation

    [Fact]
    public async Task Validate_WhenSecondaryColorExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: new string('a', 51),
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SecondaryColor)
            .WithErrorMessage("Secondary color must not exceed 50 characters.");
    }

    [Theory]
    [InlineData("invalid-color")]
    [InlineData("FF5733")]
    [InlineData("#GG5733")]
    public async Task Validate_WhenSecondaryColorHasInvalidFormat_ShouldHaveError(string color)
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: color,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SecondaryColor)
            .WithErrorMessage("Secondary color must be a valid hex color (e.g., #FF5733).");
    }

    [Theory]
    [InlineData("#333FFF")]
    [InlineData("#abc123")]
    [InlineData("#FFF")]
    public async Task Validate_WhenSecondaryColorIsValidHex_ShouldNotHaveError(string color)
    {
        // Arrange
        var command = new UpdateBrandingSettingsCommand(
            LogoUrl: null,
            FaviconUrl: null,
            PrimaryColor: null,
            SecondaryColor: color,
            DarkModeDefault: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SecondaryColor);
    }

    #endregion
}
