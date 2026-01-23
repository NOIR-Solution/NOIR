namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for ResendEmailChangeOtpCommandValidator.
/// Tests validation rules for resending email change OTP.
/// </summary>
public class ResendEmailChangeOtpCommandValidatorTests
{
    private readonly ResendEmailChangeOtpCommandValidator _validator;

    public ResendEmailChangeOtpCommandValidatorTests()
    {
        _validator = new ResendEmailChangeOtpCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected validation messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Generic validations
        mock.Setup(x => x["validation.required"]).Returns("This field is required.");

        return mock.Object;
    }

    #region SessionToken Validation

    [Fact]
    public async Task Validate_WhenSessionTokenIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ResendEmailChangeOtpCommand("");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken)
            .WithErrorMessage("This field is required.");
    }

    [Fact]
    public async Task Validate_WhenSessionTokenIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ResendEmailChangeOtpCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken);
    }

    [Fact]
    public async Task Validate_WhenSessionTokenIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new ResendEmailChangeOtpCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionToken);
    }

    #endregion

    #region Valid SessionToken Tests

    [Theory]
    [InlineData("abc123")]
    [InlineData("session-token-with-dashes")]
    [InlineData("some.session.token.value")]
    [InlineData("uuid-like-token-123e4567-e89b-12d3-a456")]
    public async Task Validate_WhenSessionTokenIsValid_ShouldNotHaveError(string token)
    {
        // Arrange
        var command = new ResendEmailChangeOtpCommand(token);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ResendEmailChangeOtpCommand("valid-session-token-12345");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
