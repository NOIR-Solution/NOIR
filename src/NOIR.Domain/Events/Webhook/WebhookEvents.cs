namespace NOIR.Domain.Events.Webhook;

public record WebhookSubscriptionCreatedEvent(Guid SubscriptionId, string Name, string Url) : DomainEvent;
public record WebhookSubscriptionActivatedEvent(Guid SubscriptionId, string Name) : DomainEvent;
public record WebhookSubscriptionDeactivatedEvent(Guid SubscriptionId, string Name) : DomainEvent;
public record WebhookDeliverySucceededEvent(Guid DeliveryLogId, Guid SubscriptionId, string EventType) : DomainEvent;
public record WebhookDeliveryFailedEvent(Guid DeliveryLogId, Guid SubscriptionId, string EventType, string? ErrorMessage) : DomainEvent;
