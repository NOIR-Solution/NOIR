namespace NOIR.Domain.Events.Review;

/// <summary>
/// Raised when a new product review is created.
/// </summary>
public record ReviewCreatedEvent(
    Guid ReviewId,
    Guid ProductId,
    string UserId,
    int Rating) : DomainEvent;

/// <summary>
/// Raised when a review is approved for public display.
/// </summary>
public record ReviewApprovedEvent(
    Guid ReviewId,
    Guid ProductId) : DomainEvent;

/// <summary>
/// Raised when a review is rejected.
/// </summary>
public record ReviewRejectedEvent(
    Guid ReviewId,
    Guid ProductId) : DomainEvent;

/// <summary>
/// Raised when an admin responds to a review.
/// </summary>
public record ReviewAdminRespondedEvent(
    Guid ReviewId,
    Guid ProductId) : DomainEvent;
