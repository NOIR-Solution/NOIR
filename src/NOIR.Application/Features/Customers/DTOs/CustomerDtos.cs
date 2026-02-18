namespace NOIR.Application.Features.Customers.DTOs;

/// <summary>
/// Full DTO for Customer entity (includes addresses).
/// Used for single-customer views and audit before-state tracking.
/// </summary>
public sealed record CustomerDto
{
    public Guid Id { get; init; }
    public string? UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public CustomerSegment Segment { get; init; }
    public CustomerTier Tier { get; init; }
    public DateTimeOffset? LastOrderDate { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int LoyaltyPoints { get; init; }
    public int LifetimeLoyaltyPoints { get; init; }
    public string? Tags { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<CustomerAddressDto> Addresses { get; init; } = [];
}

/// <summary>
/// Summary DTO for customer lists.
/// </summary>
public sealed record CustomerSummaryDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public CustomerSegment Segment { get; init; }
    public CustomerTier Tier { get; init; }
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public int LoyaltyPoints { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO for customer address.
/// </summary>
public sealed record CustomerAddressDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public AddressType AddressType { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string? Ward { get; init; }
    public string? District { get; init; }
    public string Province { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public bool IsDefault { get; init; }
}

/// <summary>
/// DTO for customer statistics (charts/dashboards).
/// </summary>
public sealed record CustomerStatsDto
{
    public int TotalCustomers { get; init; }
    public int ActiveCustomers { get; init; }
    public IReadOnlyList<SegmentDistributionDto> SegmentDistribution { get; init; } = [];
    public IReadOnlyList<TierDistributionDto> TierDistribution { get; init; } = [];
    public IReadOnlyList<CustomerSummaryDto> TopSpenders { get; init; } = [];
}

/// <summary>
/// DTO for segment distribution.
/// </summary>
public sealed record SegmentDistributionDto
{
    public CustomerSegment Segment { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// DTO for tier distribution.
/// </summary>
public sealed record TierDistributionDto
{
    public CustomerTier Tier { get; init; }
    public int Count { get; init; }
}
