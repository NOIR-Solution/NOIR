namespace NOIR.Application.Features.Notifications.Commands.DeleteNotification;

/// <summary>
/// Validator for DeleteNotificationCommand.
/// </summary>
public sealed class DeleteNotificationCommandValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required.");
    }
}
