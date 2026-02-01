using NOIR.Application.Features.Notifications.Commands.DeleteNotification;
using NOIR.Application.Features.Notifications.Commands.MarkAllAsRead;
using NOIR.Application.Features.Notifications.Commands.MarkAsRead;

namespace NOIR.Application.UnitTests.Features.Notifications.Validators;

/// <summary>
/// Unit tests for notification command validators.
/// </summary>
public class NotificationCommandValidatorTests
{
    #region DeleteNotificationCommandValidator Tests

    public class DeleteNotificationCommandValidatorTests
    {
        private readonly DeleteNotificationCommandValidator _validator = new();

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
    }

    #endregion

    #region MarkAsReadCommandValidator Tests

    public class MarkAsReadCommandValidatorTests
    {
        private readonly MarkAsReadCommandValidator _validator = new();

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
    }

    #endregion

    #region MarkAllAsReadCommandValidator Tests

    public class MarkAllAsReadCommandValidatorTests
    {
        private readonly MarkAllAsReadCommandValidator _validator = new();

        [Fact]
        public async Task Validate_WhenCommandIsValid_ShouldNotHaveError()
        {
            // Arrange
            var command = new MarkAllAsReadCommand();

            // Act
            var result = await _validator.TestValidateAsync(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion
}
