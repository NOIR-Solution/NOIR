namespace NOIR.Application.Features.Promotions.DTOs;

/// <summary>
/// Mapper for Promotion-related entities to DTOs.
/// </summary>
public static class PromotionMapper
{
    /// <summary>
    /// Maps a Promotion entity to PromotionDto including related data.
    /// </summary>
    public static PromotionDto ToDto(Domain.Entities.Promotion.Promotion promotion)
    {
        return new PromotionDto
        {
            Id = promotion.Id,
            Name = promotion.Name,
            Description = promotion.Description,
            Code = promotion.Code,
            PromotionType = promotion.PromotionType,
            DiscountType = promotion.DiscountType,
            DiscountValue = promotion.DiscountValue,
            MaxDiscountAmount = promotion.MaxDiscountAmount,
            MinOrderValue = promotion.MinOrderValue,
            MinItemQuantity = promotion.MinItemQuantity,
            UsageLimitTotal = promotion.UsageLimitTotal,
            UsageLimitPerUser = promotion.UsageLimitPerUser,
            CurrentUsageCount = promotion.CurrentUsageCount,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            IsActive = promotion.IsActive,
            Status = promotion.Status,
            ApplyLevel = promotion.ApplyLevel,
            ProductIds = promotion.Products.Select(p => p.ProductId).ToList(),
            CategoryIds = promotion.Categories.Select(c => c.CategoryId).ToList(),
            RecentUsages = promotion.Usages
                .OrderByDescending(u => u.UsedAt)
                .Take(10)
                .Select(ToUsageDto)
                .ToList(),
            CreatedAt = promotion.CreatedAt,
            ModifiedAt = promotion.ModifiedAt
        };
    }

    /// <summary>
    /// Maps a PromotionUsage entity to PromotionUsageDto.
    /// </summary>
    public static PromotionUsageDto ToUsageDto(Domain.Entities.Promotion.PromotionUsage usage)
    {
        return new PromotionUsageDto
        {
            Id = usage.Id,
            PromotionId = usage.PromotionId,
            UserId = usage.UserId,
            OrderId = usage.OrderId,
            DiscountAmount = usage.DiscountAmount,
            UsedAt = usage.UsedAt
        };
    }
}
