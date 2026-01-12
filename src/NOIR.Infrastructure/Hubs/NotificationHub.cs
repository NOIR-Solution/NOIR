namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time notification delivery.
/// Uses strongly-typed client interface to avoid magic strings.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects. Adds user to their personal group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group for targeted notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects. Removes user from their group.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        if (exception != null)
        {
            _logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to join role-based groups for broadcast notifications.
    /// </summary>
    public async Task JoinRoleGroup(string roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{roleName}");
            _logger.LogDebug("Connection {ConnectionId} joined role group {RoleName}",
                Context.ConnectionId, roleName);
        }
    }

    /// <summary>
    /// Allows clients to leave role-based groups.
    /// </summary>
    public async Task LeaveRoleGroup(string roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{roleName}");
            _logger.LogDebug("Connection {ConnectionId} left role group {RoleName}",
                Context.ConnectionId, roleName);
        }
    }
}
