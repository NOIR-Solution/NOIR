namespace NOIR.Application.Features.Subscriptions.DTOs;

/// <summary>
/// DTO for subscription plan.
/// </summary>
public record SubscriptionPlanDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public string Interval { get; init; } = string.Empty;
    public int? TrialDays { get; init; }
    public List<string>? Features { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? ExternalPlanId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO for creating a subscription plan.
/// </summary>
public record CreateSubscriptionPlanDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public BillingInterval Interval { get; init; }
    public int? TrialDays { get; init; }
    public List<string>? Features { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>
/// DTO for updating a subscription plan.
/// </summary>
public record UpdateSubscriptionPlanDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public List<string>? Features { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
