namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for LogoutCommandValidator.
/// </summary>
public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator;

    public LogoutCommandValidatorTests()
    {
        _validator = new LogoutCommandValidator();
    }

    [Fact]
    public async Task Validate_WithDefaultValues_ShouldNotHaveAnyErrors()
    {
        // Arrange - LogoutCommand has optional parameters with default values
        var command = new LogoutCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert - No validation rules, so should always pass
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithRefreshToken_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LogoutCommand("valid-refresh-token");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithRevokeAllSessions_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LogoutCommand(RevokeAllSessions: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithAllParameters_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new LogoutCommand("refresh-token", RevokeAllSessions: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
