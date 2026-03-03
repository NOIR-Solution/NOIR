namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Implementation of IEntityUpdateHubContext using SignalR.
/// Publishes entity update signals to list and instance subscriber groups.
/// </summary>
public class EntityUpdateHubContext : IEntityUpdateHubContext, IScopedService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<EntityUpdateHubContext> _logger;

    public EntityUpdateHubContext(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<EntityUpdateHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishEntityUpdatedAsync(
        string entityType,
        Guid entityId,
        EntityOperation operation,
        string tenantId,
        CancellationToken ct = default)
    {
        var signal = new EntityUpdateSignal(
            entityType,
            entityId.ToString(),
            operation,
            DateTimeOffset.UtcNow);

        try
        {
            await _hubContext.Clients
                .Group($"entity_list_{entityType}_{tenantId}")
                .EntityCollectionUpdated(signal);

            await _hubContext.Clients
                .Group($"entity_{entityType}_{entityId}_{tenantId}")
                .EntityUpdated(signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish entity update signal for {EntityType} {EntityId}",
                entityType, entityId);
        }
    }
}
