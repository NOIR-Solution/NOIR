namespace NOIR.Domain.Events.Customer;

/// <summary>
/// Raised when a new customer is created.
/// </summary>
public record CustomerCreatedEvent(
    Guid CustomerId,
    string Email,
    string FirstName,
    string LastName) : DomainEvent;

/// <summary>
/// Raised when a customer's profile is updated.
/// </summary>
public record CustomerUpdatedEvent(
    Guid CustomerId,
    string Email) : DomainEvent;

/// <summary>
/// Raised when a customer is deactivated.
/// </summary>
public record CustomerDeactivatedEvent(
    Guid CustomerId,
    string Email) : DomainEvent;

/// <summary>
/// Raised when a customer's segment changes after RFM recalculation.
/// </summary>
public record CustomerSegmentChangedEvent(
    Guid CustomerId,
    CustomerSegment OldSegment,
    CustomerSegment NewSegment) : DomainEvent;

/// <summary>
/// Raised when a customer's loyalty tier changes.
/// </summary>
public record CustomerTierChangedEvent(
    Guid CustomerId,
    CustomerTier OldTier,
    CustomerTier NewTier) : DomainEvent;

/// <summary>
/// Raised when loyalty points are added to a customer.
/// </summary>
public record CustomerLoyaltyPointsAddedEvent(
    Guid CustomerId,
    int Points,
    int NewBalance) : DomainEvent;

/// <summary>
/// Raised when a customer redeems loyalty points.
/// </summary>
public record CustomerLoyaltyPointsRedeemedEvent(
    Guid CustomerId,
    int Points,
    int NewBalance) : DomainEvent;
