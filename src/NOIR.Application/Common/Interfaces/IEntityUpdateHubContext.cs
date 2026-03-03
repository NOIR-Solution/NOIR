namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing real-time entity update signals via SignalR.
/// Notifies clients subscribed to entity list or instance groups.
/// </summary>
public interface IEntityUpdateHubContext
{
    Task PublishEntityUpdatedAsync(
        string entityType,
        Guid entityId,
        EntityOperation operation,
        string tenantId,
        CancellationToken ct = default);
}
