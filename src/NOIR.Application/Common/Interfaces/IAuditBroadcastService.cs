namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for broadcasting audit events to connected SignalR clients.
/// </summary>
public interface IAuditBroadcastService
{
    /// <summary>
    /// Broadcasts an HTTP request audit event to relevant subscribers.
    /// </summary>
    Task BroadcastHttpRequestAuditAsync(HttpRequestAuditEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts a handler execution audit event to relevant subscribers.
    /// </summary>
    Task BroadcastHandlerAuditAsync(HandlerAuditEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts an entity change audit event to relevant subscribers.
    /// </summary>
    Task BroadcastEntityAuditAsync(EntityAuditEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts updated statistics to all dashboard subscribers.
    /// </summary>
    Task BroadcastStatsUpdateAsync(AuditStatsUpdate stats, CancellationToken ct = default);
}
