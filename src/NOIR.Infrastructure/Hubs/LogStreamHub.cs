namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time log streaming to developer clients.
/// Only accessible by authenticated admin users.
///
/// Note: Filtering is handled client-side to avoid scalability issues with
/// per-connection server-side state. All logs are broadcast to all clients.
/// </summary>
[Authorize(Policy = "system:admin")]
public class LogStreamHub : Hub<ILogStreamClient>
{
    private readonly ILogger<LogStreamHub> _logger;
    private readonly ILogRingBuffer _logBuffer;
    private readonly ILogLevelService _logLevelService;

    // Thread-safe connection tracking using ConcurrentDictionary (no locks needed)
    private static readonly ConcurrentDictionary<string, byte> _connections = new();

    public LogStreamHub(
        ILogger<LogStreamHub> logger,
        ILogRingBuffer logBuffer,
        ILogLevelService logLevelService)
    {
        _logger = logger;
        _logBuffer = logBuffer;
        _logLevelService = logLevelService;
    }

    /// <summary>
    /// Called when a client connects. Adds to log stream group and sends initial data.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var connectionId = Context.ConnectionId;

        // Add to log stream group
        await Groups.AddToGroupAsync(connectionId, "log_stream");

        // Track connection
        _connections.TryAdd(connectionId, 0);

        _logger.LogInformation(
            "Admin user {UserId} connected to LogStreamHub. Active connections: {ConnectionCount}",
            userId, _connections.Count);

        // Send current log level
        var currentLevel = _logLevelService.GetCurrentLevel();
        await Clients.Caller.ReceiveLevelChanged(currentLevel);

        // Send buffer stats
        var stats = _logBuffer.GetStats();
        await Clients.Caller.ReceiveBufferStats(stats);

        // Send recent logs (last 100 by default)
        var recentLogs = _logBuffer.GetRecentEntries(100);
        if (recentLogs.Any())
        {
            await Clients.Caller.ReceiveLogBatch(recentLogs);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var connectionId = Context.ConnectionId;

        // Remove from log stream group
        await Groups.RemoveFromGroupAsync(connectionId, "log_stream");

        // Remove from tracking
        _connections.TryRemove(connectionId, out _);

        _logger.LogInformation(
            "Admin user {UserId} disconnected from LogStreamHub. Active connections: {ConnectionCount}",
            userId, _connections.Count);

        if (exception != null)
        {
            _logger.LogWarning(exception, "LogStreamHub disconnection with error for user {UserId}", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to request more historical entries.
    /// </summary>
    public async Task RequestHistory(int count, long? beforeId = null)
    {
        var entries = beforeId.HasValue
            ? _logBuffer.GetEntriesBefore(beforeId.Value, count)
            : _logBuffer.GetRecentEntries(count);

        await Clients.Caller.ReceiveLogBatch(entries);
    }

    /// <summary>
    /// Allows clients to request error summary.
    /// </summary>
    public async Task RequestErrorSummary(int maxClusters = 10)
    {
        var clusters = _logBuffer.GetErrorClusters(maxClusters);
        await Clients.Caller.ReceiveErrorSummary(clusters);
    }

    /// <summary>
    /// Allows clients to request current buffer statistics.
    /// </summary>
    public async Task RequestBufferStats()
    {
        var stats = _logBuffer.GetStats();
        await Clients.Caller.ReceiveBufferStats(stats);
    }

    /// <summary>
    /// Clears the in-memory log buffer (admin action).
    /// </summary>
    public async Task ClearBuffer()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogWarning("Admin user {UserId} cleared the log buffer", userId);

        _logBuffer.Clear();

        // Notify all connected clients
        var stats = _logBuffer.GetStats();
        await Clients.Group("log_stream").ReceiveBufferStats(stats);
    }

    /// <summary>
    /// Gets all connection IDs currently subscribed.
    /// </summary>
    public static IEnumerable<string> GetAllConnectionIds()
    {
        return _connections.Keys;
    }

    /// <summary>
    /// Gets the count of active connections.
    /// </summary>
    public static int GetConnectionCount()
    {
        return _connections.Count;
    }
}

/// <summary>
/// Filter options for log streaming (used by frontend for client-side filtering).
/// </summary>
public class LogStreamFilter
{
    /// <summary>
    /// Minimum log level to display.
    /// </summary>
    public DevLogLevel MinLevel { get; set; } = DevLogLevel.Information;

    /// <summary>
    /// Source contexts to include (null = all).
    /// </summary>
    public string[]? Sources { get; set; }

    /// <summary>
    /// Search pattern to match (null = all).
    /// </summary>
    public string? SearchPattern { get; set; }

    /// <summary>
    /// Whether to show only entries with exceptions.
    /// </summary>
    public bool ExceptionsOnly { get; set; }
}
