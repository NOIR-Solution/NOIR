namespace NOIR.Application.UnitTests.Features.Notifications.Validators;

/// <summary>
/// Unit tests for MarkAllAsReadCommandValidator.
/// </summary>
public class MarkAllAsReadCommandValidatorTests
{
    private readonly MarkAllAsReadCommandValidator _validator;

    public MarkAllAsReadCommandValidatorTests()
    {
        _validator = new MarkAllAsReadCommandValidator();
    }

    [Fact]
    public async Task Validate_WithDefaultCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange - MarkAllAsReadCommand has no required parameters
        var command = new MarkAllAsReadCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert - No validation rules, so should always pass
        result.ShouldNotHaveAnyValidationErrors();
    }
}
