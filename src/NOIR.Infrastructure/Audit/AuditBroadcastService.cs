using Microsoft.AspNetCore.SignalR;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Implementation of IAuditBroadcastService that broadcasts events via SignalR.
/// Uses IHubContext to send messages from outside the Hub.
/// </summary>
public class AuditBroadcastService : IAuditBroadcastService, IScopedService
{
    private readonly IHubContext<AuditHub, IAuditHubClient> _hubContext;
    private readonly ILogger<AuditBroadcastService> _logger;

    // Group name patterns (must match AuditHub)
    private const string AllEventsGroup = "audit:all";
    private const string TenantGroupPrefix = "audit:tenant:";
    private const string EntityTypeGroupPrefix = "audit:entity:";
    private const string DashboardGroup = "audit:dashboard";

    public AuditBroadcastService(
        IHubContext<AuditHub, IAuditHubClient> hubContext,
        ILogger<AuditBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BroadcastHttpRequestAuditAsync(HttpRequestAuditEvent evt, CancellationToken ct = default)
    {
        try
        {
            var tasks = new List<Task>();

            // Broadcast to all events subscribers
            tasks.Add(_hubContext.Clients.Group(AllEventsGroup).ReceiveHttpRequestAudit(evt));

            // Broadcast to tenant subscribers
            if (!string.IsNullOrEmpty(evt.TenantId))
            {
                var tenantGroup = $"{TenantGroupPrefix}{evt.TenantId}";
                tasks.Add(_hubContext.Clients.Group(tenantGroup).ReceiveHttpRequestAudit(evt));
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug(
                "Broadcasted HTTP request audit event. CorrelationId: {CorrelationId}",
                evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to broadcast HTTP request audit event. CorrelationId: {CorrelationId}",
                evt.CorrelationId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastHandlerAuditAsync(HandlerAuditEvent evt, CancellationToken ct = default)
    {
        try
        {
            var tasks = new List<Task>();

            // Broadcast to all events subscribers
            tasks.Add(_hubContext.Clients.Group(AllEventsGroup).ReceiveHandlerAudit(evt));

            await Task.WhenAll(tasks);

            _logger.LogDebug(
                "Broadcasted handler audit event. Handler: {HandlerName}, CorrelationId: {CorrelationId}",
                evt.HandlerName, evt.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to broadcast handler audit event. CorrelationId: {CorrelationId}",
                evt.CorrelationId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastEntityAuditAsync(EntityAuditEvent evt, CancellationToken ct = default)
    {
        try
        {
            var tasks = new List<Task>();

            // Broadcast to all events subscribers
            tasks.Add(_hubContext.Clients.Group(AllEventsGroup).ReceiveEntityAudit(evt));

            // Broadcast to entity type subscribers
            if (!string.IsNullOrEmpty(evt.EntityType))
            {
                var entityTypeGroup = $"{EntityTypeGroupPrefix}{evt.EntityType}";
                tasks.Add(_hubContext.Clients.Group(entityTypeGroup).ReceiveEntityAudit(evt));
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug(
                "Broadcasted entity audit event. EntityType: {EntityType}, EntityId: {EntityId}",
                evt.EntityType, evt.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to broadcast entity audit event. EntityType: {EntityType}",
                evt.EntityType);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastStatsUpdateAsync(AuditStatsUpdate stats, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group(DashboardGroup).ReceiveStatsUpdate(stats);

            _logger.LogDebug(
                "Broadcasted stats update. Requests: {Requests}, Changes: {Changes}",
                stats.TodayHttpRequests, stats.TodayEntityChanges);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast stats update");
        }
    }
}
