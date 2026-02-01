namespace NOIR.Application.UnitTests.Features.Notifications.Validators;

/// <summary>
/// Unit tests for MarkAsReadCommandValidator.
/// </summary>
public class MarkAsReadCommandValidatorTests
{
    private readonly MarkAsReadCommandValidator _validator;

    public MarkAsReadCommandValidatorTests()
    {
        _validator = new MarkAsReadCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenNotificationIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new MarkAsReadCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NotificationId)
            .WithErrorMessage("Notification ID is required.");
    }

    [Fact]
    public async Task Validate_WhenNotificationIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new MarkAsReadCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
