namespace NOIR.Application.Features.Notifications.Commands.MarkAllAsRead;

/// <summary>
/// Validator for MarkAllAsReadCommand.
/// </summary>
public sealed class MarkAllAsReadCommandValidator : AbstractValidator<MarkAllAsReadCommand>
{
    public MarkAllAsReadCommandValidator()
    {
        // No validation needed - marks all notifications as read for the current user.
    }
}
