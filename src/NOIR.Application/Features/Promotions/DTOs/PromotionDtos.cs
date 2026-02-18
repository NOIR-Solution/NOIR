namespace NOIR.Application.Features.Promotions.DTOs;

/// <summary>
/// DTO for Promotion entity including related products, categories, and usage stats.
/// </summary>
public sealed record PromotionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Code { get; init; } = string.Empty;
    public PromotionType PromotionType { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderValue { get; init; }
    public int? MinItemQuantity { get; init; }
    public int? UsageLimitTotal { get; init; }
    public int? UsageLimitPerUser { get; init; }
    public int CurrentUsageCount { get; init; }
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public bool IsActive { get; init; }
    public PromotionStatus Status { get; init; }
    public PromotionApplyLevel ApplyLevel { get; init; }
    public IReadOnlyList<Guid> ProductIds { get; init; } = [];
    public IReadOnlyList<Guid> CategoryIds { get; init; } = [];
    public IReadOnlyList<PromotionUsageDto> RecentUsages { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
}

/// <summary>
/// DTO for promotion usage records.
/// </summary>
public sealed record PromotionUsageDto
{
    public Guid Id { get; init; }
    public Guid PromotionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateTimeOffset UsedAt { get; init; }
}

/// <summary>
/// Result DTO for promo code validation.
/// </summary>
public sealed record PromoCodeValidationDto
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public decimal DiscountAmount { get; init; }
    public string Code { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
}
