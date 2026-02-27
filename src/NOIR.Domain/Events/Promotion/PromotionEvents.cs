namespace NOIR.Domain.Events.Promotion;

/// <summary>
/// Raised when a new promotion is created.
/// </summary>
public record PromotionCreatedEvent(
    Guid PromotionId,
    string Code,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a promotion is activated.
/// </summary>
public record PromotionActivatedEvent(
    Guid PromotionId,
    string Code) : DomainEvent;

/// <summary>
/// Raised when a promotion is deactivated.
/// </summary>
public record PromotionDeactivatedEvent(
    Guid PromotionId,
    string Code) : DomainEvent;

/// <summary>
/// Raised when a promotion is applied (usage incremented).
/// </summary>
public record PromotionAppliedEvent(
    Guid PromotionId,
    string Code,
    int NewUsageCount) : DomainEvent;
