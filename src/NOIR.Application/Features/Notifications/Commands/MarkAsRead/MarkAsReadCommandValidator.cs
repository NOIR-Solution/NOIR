namespace NOIR.Application.Features.Notifications.Commands.MarkAsRead;

/// <summary>
/// Validator for MarkAsReadCommand.
/// </summary>
public sealed class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
{
    public MarkAsReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("Notification ID is required.");
    }
}
