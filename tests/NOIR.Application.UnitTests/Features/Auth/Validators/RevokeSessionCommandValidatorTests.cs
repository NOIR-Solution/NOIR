namespace NOIR.Application.UnitTests.Features.Auth.Validators;

/// <summary>
/// Unit tests for RevokeSessionCommandValidator.
/// </summary>
public class RevokeSessionCommandValidatorTests
{
    private readonly RevokeSessionCommandValidator _validator;

    public RevokeSessionCommandValidatorTests()
    {
        _validator = new RevokeSessionCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenSessionIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RevokeSessionCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId)
            .WithErrorMessage("Session ID is required.");
    }

    [Fact]
    public async Task Validate_WhenSessionIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RevokeSessionCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSessionIdIsValidWithIpAddress_ShouldNotHaveError()
    {
        // Arrange
        var command = new RevokeSessionCommand(Guid.NewGuid(), "192.168.1.1");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
