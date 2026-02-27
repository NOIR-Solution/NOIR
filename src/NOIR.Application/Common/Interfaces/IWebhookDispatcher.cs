namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Dispatches domain events to matching webhook subscriptions.
/// Finds all active subscriptions whose event patterns match the event type,
/// creates delivery logs, and publishes delivery commands via the message bus.
/// </summary>
public interface IWebhookDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
