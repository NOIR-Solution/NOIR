using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// SignalR Hub for real-time audit event streaming.
/// Supports tenant-based filtering and multiple subscription types.
/// </summary>
[Authorize]
public class AuditHub : Hub<IAuditHubClient>
{
    private readonly IAuditStatsService _statsService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditHub> _logger;

    // Group name patterns
    private const string AllEventsGroup = "audit:all";
    private const string TenantGroupPrefix = "audit:tenant:";
    private const string EntityTypeGroupPrefix = "audit:entity:";
    private const string DashboardGroup = "audit:dashboard";

    public AuditHub(
        IAuditStatsService statsService,
        ICurrentUser currentUser,
        ILogger<AuditHub> logger)
    {
        _statsService = statsService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically subscribes to tenant group if tenant context exists.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;
        var connectionId = Context.ConnectionId;

        _logger.LogInformation(
            "Audit client connected. ConnectionId: {ConnectionId}, UserId: {UserId}, TenantId: {TenantId}",
            connectionId, userId, tenantId);

        // Auto-subscribe to tenant group
        var groups = new List<string>();
        if (!string.IsNullOrEmpty(tenantId))
        {
            var tenantGroup = $"{TenantGroupPrefix}{tenantId}";
            await Groups.AddToGroupAsync(connectionId, tenantGroup);
            groups.Add(tenantGroup);
        }

        // Get initial stats for the connection
        var initialStats = await _statsService.GetCurrentStatsAsync(tenantId);

        // Send connection confirmation with initial state
        await Clients.Caller.ReceiveConnectionConfirmed(new AuditConnectionInfo(
            connectionId,
            tenantId,
            groups,
            initialStats));

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Audit client disconnected with error. ConnectionId: {ConnectionId}",
                connectionId);
        }
        else
        {
            _logger.LogInformation(
                "Audit client disconnected. ConnectionId: {ConnectionId}",
                connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to all audit events (requires appropriate permissions).
    /// </summary>
    public async Task SubscribeToAllEvents()
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, AllEventsGroup);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to all events",
            connectionId);
    }

    /// <summary>
    /// Unsubscribe from all audit events.
    /// </summary>
    public async Task UnsubscribeFromAllEvents()
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, AllEventsGroup);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from all events",
            connectionId);
    }

    /// <summary>
    /// Subscribe to events for a specific entity type.
    /// </summary>
    public async Task SubscribeToEntityType(string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        var connectionId = Context.ConnectionId;
        var groupName = $"{EntityTypeGroupPrefix}{entityType}";
        await Groups.AddToGroupAsync(connectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to entity type {EntityType}",
            connectionId, entityType);
    }

    /// <summary>
    /// Unsubscribe from events for a specific entity type.
    /// </summary>
    public async Task UnsubscribeFromEntityType(string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        var connectionId = Context.ConnectionId;
        var groupName = $"{EntityTypeGroupPrefix}{entityType}";
        await Groups.RemoveFromGroupAsync(connectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from entity type {EntityType}",
            connectionId, entityType);
    }

    /// <summary>
    /// Subscribe to dashboard statistics updates.
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        var connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, DashboardGroup);

        // Send immediate stats update
        var tenantId = _currentUser.TenantId;
        var stats = await _statsService.GetCurrentStatsAsync(tenantId);
        await Clients.Caller.ReceiveStatsUpdate(stats);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to dashboard updates",
            connectionId);
    }

    /// <summary>
    /// Unsubscribe from dashboard statistics updates.
    /// </summary>
    public async Task UnsubscribeFromDashboard()
    {
        var connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, DashboardGroup);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from dashboard updates",
            connectionId);
    }

    /// <summary>
    /// Request a manual stats refresh.
    /// </summary>
    public async Task RequestStatsRefresh()
    {
        var tenantId = _currentUser.TenantId;
        var stats = await _statsService.GetCurrentStatsAsync(tenantId);
        await Clients.Caller.ReceiveStatsUpdate(stats);
    }
}
