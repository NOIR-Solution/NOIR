namespace NOIR.Application.Features.Subscriptions.DTOs;

/// <summary>
/// DTO for subscription.
/// </summary>
public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CurrentPeriodStart { get; init; }
    public DateTimeOffset CurrentPeriodEnd { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public DateTimeOffset? TrialEnd { get; init; }
    public string Interval { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "VND";
    public string? ExternalSubscriptionId { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO for creating a subscription.
/// </summary>
public record CreateSubscriptionDto
{
    public Guid CustomerId { get; init; }
    public Guid PlanId { get; init; }
    public decimal? DiscountedAmount { get; init; }
}

/// <summary>
/// DTO for subscription status change.
/// </summary>
public record SubscriptionStatusChangeDto
{
    public Guid SubscriptionId { get; init; }
    public SubscriptionStatus NewStatus { get; init; }
    public string? Reason { get; init; }
}
