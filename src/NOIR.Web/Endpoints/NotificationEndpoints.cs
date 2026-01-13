namespace NOIR.Web.Endpoints;

using NOIR.Application.Features.Notifications.Commands.DeleteNotification;
using NOIR.Application.Features.Notifications.Commands.MarkAllAsRead;
using NOIR.Application.Features.Notifications.Commands.MarkAsRead;
using NOIR.Application.Features.Notifications.Commands.UpdatePreferences;
using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Application.Features.Notifications.Queries.GetNotifications;
using NOIR.Application.Features.Notifications.Queries.GetPreferences;
using NOIR.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Notification API endpoints.
/// Provides CRUD operations for notifications and user preferences.
/// </summary>
public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireRateLimiting("fixed");

        // GET /api/notifications - Get paginated notifications
        group.MapGet("/", async (
            IMessageBus bus,
            int page = 1,
            int pageSize = 20,
            bool includeRead = true) =>
        {
            var query = new GetNotificationsQuery(page, pageSize, includeRead);
            var result = await bus.InvokeAsync<Result<NotificationsPagedResponse>>(query);
            return result.ToHttpResult();
        })
        .WithName("GetNotifications")
        .WithSummary("Get paginated notifications")
        .WithDescription("Returns the current user's notifications in chronological order (newest first).")
        .Produces<NotificationsPagedResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // GET /api/notifications/unread-count - Get unread notification count
        group.MapGet("/unread-count", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<UnreadCountResponse>>(new GetUnreadCountQuery());
            return result.ToHttpResult();
        })
        .WithName("GetUnreadCount")
        .WithSummary("Get unread notification count")
        .WithDescription("Returns the count of unread notifications for the current user.")
        .Produces<UnreadCountResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // POST /api/notifications/{id}/read - Mark single notification as read
        group.MapPost("/{id:guid}/read", async (Guid id, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(new MarkAsReadCommand(id));
            return result.ToHttpResult();
        })
        .WithName("MarkNotificationAsRead")
        .WithSummary("Mark notification as read")
        .WithDescription("Marks a specific notification as read.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // POST /api/notifications/read-all - Mark all notifications as read
        group.MapPost("/read-all", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<int>>(new MarkAllAsReadCommand());
            return result.ToHttpResult();
        })
        .WithName("MarkAllNotificationsAsRead")
        .WithSummary("Mark all notifications as read")
        .WithDescription("Marks all unread notifications as read for the current user. Returns the count of notifications marked.")
        .Produces<int>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // DELETE /api/notifications/{id} - Delete notification
        group.MapDelete("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result>(new DeleteNotificationCommand(id));
            return result.ToHttpResult();
        })
        .WithName("DeleteNotification")
        .WithSummary("Delete notification")
        .WithDescription("Soft-deletes a notification.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // GET /api/notifications/preferences - Get notification preferences
        group.MapGet("/preferences", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IEnumerable<NotificationPreferenceDto>>>(
                new GetPreferencesQuery());
            return result.ToHttpResult();
        })
        .WithName("GetNotificationPreferences")
        .WithSummary("Get notification preferences")
        .WithDescription("Returns the current user's notification preferences for all categories.")
        .Produces<IEnumerable<NotificationPreferenceDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        // PUT /api/notifications/preferences - Update notification preferences
        group.MapPut("/preferences", async (
            UpdatePreferencesCommand command,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<IEnumerable<NotificationPreferenceDto>>>(command);
            return result.ToHttpResult();
        })
        .WithName("UpdateNotificationPreferences")
        .WithSummary("Update notification preferences")
        .WithDescription("Updates notification preferences for specified categories.")
        .Produces<IEnumerable<NotificationPreferenceDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
    }
}
