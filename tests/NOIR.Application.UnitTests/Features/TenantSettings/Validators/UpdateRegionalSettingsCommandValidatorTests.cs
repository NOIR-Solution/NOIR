namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

/// <summary>
/// Unit tests for UpdateRegionalSettingsCommandValidator.
/// Tests validation rules for updating regional settings.
/// </summary>
public class UpdateRegionalSettingsCommandValidatorTests
{
    private readonly UpdateRegionalSettingsCommandValidator _validator;

    public UpdateRegionalSettingsCommandValidatorTests()
    {
        _validator = new UpdateRegionalSettingsCommandValidator();
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "America/New_York",
            Language: "en",
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("en", "YYYY-MM-DD")]
    [InlineData("vi", "MM/DD/YYYY")]
    [InlineData("ja", "DD/MM/YYYY")]
    [InlineData("ko", "DD.MM.YYYY")]
    [InlineData("zh", "YYYY-MM-DD")]
    [InlineData("fr", "DD/MM/YYYY")]
    [InlineData("de", "DD.MM.YYYY")]
    [InlineData("es", "DD/MM/YYYY")]
    [InlineData("it", "DD/MM/YYYY")]
    [InlineData("pt", "DD/MM/YYYY")]
    public async Task Validate_WhenLanguageAndDateFormatAreValid_ShouldNotHaveAnyErrors(string language, string dateFormat)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: language,
            DateFormat: dateFormat);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Timezone Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenTimezoneIsEmptyOrWhitespace_ShouldHaveError(string? timezone)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: timezone!,
            Language: "en",
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timezone)
            .WithErrorMessage("Timezone is required.");
    }

    [Fact]
    public async Task Validate_WhenTimezoneExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: new string('a', 101),
            Language: "en",
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timezone)
            .WithErrorMessage("Timezone must not exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WhenTimezoneIs100Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: new string('a', 100),
            Language: "en",
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Timezone);
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/New_York")]
    [InlineData("Europe/London")]
    [InlineData("Asia/Tokyo")]
    public async Task Validate_WhenTimezoneIsValid_ShouldNotHaveError(string timezone)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: timezone,
            Language: "en",
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Timezone);
    }

    #endregion

    #region Language Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenLanguageIsEmptyOrWhitespace_ShouldHaveError(string? language)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: language!,
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Language);
    }

    [Theory]
    [InlineData("xx")]
    [InlineData("english")]
    [InlineData("EN")]
    [InlineData("abc")]
    [InlineData("zz")]
    public async Task Validate_WhenLanguageIsNotSupported_ShouldHaveError(string language)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: language,
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Language)
            .WithErrorMessage("Language must be a supported language code.");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("vi")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("zh")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt")]
    public async Task Validate_WhenLanguageIsValid_ShouldNotHaveError(string language)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: language,
            DateFormat: "YYYY-MM-DD");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Language);
    }

    #endregion

    #region DateFormat Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenDateFormatIsEmptyOrWhitespace_ShouldHaveError(string? dateFormat)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: "en",
            DateFormat: dateFormat!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateFormat);
    }

    [Theory]
    [InlineData("yyyy-mm-dd")]
    [InlineData("DD-MM-YYYY")]
    [InlineData("MM-DD-YYYY")]
    [InlineData("invalid")]
    [InlineData("YYYY/MM/DD")]
    public async Task Validate_WhenDateFormatIsNotSupported_ShouldHaveError(string dateFormat)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: "en",
            DateFormat: dateFormat);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateFormat)
            .WithErrorMessage("Date format must be one of: YYYY-MM-DD, MM/DD/YYYY, DD/MM/YYYY, DD.MM.YYYY.");
    }

    [Theory]
    [InlineData("YYYY-MM-DD")]
    [InlineData("MM/DD/YYYY")]
    [InlineData("DD/MM/YYYY")]
    [InlineData("DD.MM.YYYY")]
    public async Task Validate_WhenDateFormatIsValid_ShouldNotHaveError(string dateFormat)
    {
        // Arrange
        var command = new UpdateRegionalSettingsCommand(
            Timezone: "UTC",
            Language: "en",
            DateFormat: dateFormat);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateFormat);
    }

    #endregion
}
