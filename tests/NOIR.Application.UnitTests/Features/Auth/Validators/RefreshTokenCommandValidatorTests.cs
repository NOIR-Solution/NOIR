namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for RefreshTokenCommandValidator.
/// Tests validation rules for token refresh.
/// </summary>
public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator;

    public RefreshTokenCommandValidatorTests()
    {
        _validator = new RefreshTokenCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Token validations
        mock.Setup(x => x["validation.accessToken.required"]).Returns("Access token is required.");
        mock.Setup(x => x["validation.refreshToken.required"]).Returns("Refresh token is required when not using cookies.");

        return mock.Object;
    }

    #region AccessToken Validation

    [Fact]
    public async Task Validate_WhenAccessTokenIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "valid-refresh-token");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken)
            .WithErrorMessage("Access token is required.");
    }

    [Fact]
    public async Task Validate_WhenAccessTokenIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand(null!, "valid-refresh-token");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
    }

    [Fact]
    public async Task Validate_WhenAccessTokenIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("   ", "valid-refresh-token");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
    }

    [Fact]
    public async Task Validate_WhenAccessTokenIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "valid-refresh-token");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccessToken);
    }

    #endregion

    #region RefreshToken Validation - Without Cookies

    [Fact]
    public async Task Validate_WhenRefreshTokenIsEmptyAndNotUsingCookies_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", "", UseCookies: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
            .WithErrorMessage("Refresh token is required when not using cookies.");
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsNullAndNotUsingCookies_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", null, UseCookies: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsValidAndNotUsingCookies_ShouldNotHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", "valid-refresh-token", UseCookies: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }

    #endregion

    #region RefreshToken Validation - With Cookies

    [Fact]
    public async Task Validate_WhenRefreshTokenIsEmptyButUsingCookies_ShouldNotHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", "", UseCookies: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public async Task Validate_WhenRefreshTokenIsNullButUsingCookies_ShouldNotHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", null, UseCookies: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RefreshToken);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValidWithTokens_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand(
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U",
            "dGhpcyBpcyBhIHZhbGlkIHJlZnJlc2ggdG9rZW4=");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithCookies_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-access-token", null, UseCookies: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public async Task Validate_WhenBothTokensAreEmptyAndNotUsingCookies_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "", UseCookies: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccessToken);
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    #endregion
}
