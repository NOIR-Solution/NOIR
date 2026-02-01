namespace NOIR.Application.UnitTests.Features.Notifications.Validators;

/// <summary>
/// Unit tests for DeleteNotificationCommandValidator.
/// </summary>
public class DeleteNotificationCommandValidatorTests
{
    private readonly DeleteNotificationCommandValidator _validator;

    public DeleteNotificationCommandValidatorTests()
    {
        _validator = new DeleteNotificationCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenNotificationIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteNotificationCommand(Guid.Empty);

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
        var command = new DeleteNotificationCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
