namespace NOIR.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Query to get paginated notifications for the current user.
/// </summary>
/// <param name="Page">Page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="IncludeRead">Whether to include read notifications.</param>
public sealed record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool IncludeRead = true);
