namespace NOIR.Infrastructure.Persistence.SeedData.Data;

/// <summary>
/// Promotion definition for seed data.
/// </summary>
public record PromotionDef(
    string Name,
    string Code,
    PromotionType Type,
    DiscountType DiscountType,
    decimal DiscountValue,
    int StartDayOffset,
    int EndDayOffset,
    decimal? MinOrderValue,
    int? UsageLimitTotal,
    PromotionApplyLevel ApplyLevel,
    bool ShouldActivate);

/// <summary>
/// Vietnamese promotion seed data: voucher codes, flash sales, and free shipping campaigns.
/// Prices and thresholds in VND.
/// </summary>
public static class EngagementData
{
    /// <summary>
    /// 4 promotions covering common e-commerce campaign types.
    /// Date offsets are relative to SeedDataConstants.BaseTimestamp (2026-01-01).
    /// </summary>
    public static PromotionDef[] GetPromotions() =>
    [
        // Active voucher: 10% off, min 200K VND, 100 uses
        new("Giảm 10% đơn hàng", "GIAM10",
            PromotionType.VoucherCode, DiscountType.Percentage, 10m,
            StartDayOffset: -30, EndDayOffset: 90,
            MinOrderValue: 200_000m, UsageLimitTotal: 100,
            PromotionApplyLevel.Cart, ShouldActivate: true),

        // Active free shipping: min 500K VND, 200 uses
        new("Miễn phí vận chuyển", "FREESHIP",
            PromotionType.FreeShipping, DiscountType.FreeShipping, 30_000m,
            StartDayOffset: -30, EndDayOffset: 90,
            MinOrderValue: 500_000m, UsageLimitTotal: 200,
            PromotionApplyLevel.Cart, ShouldActivate: true),

        // Scheduled: Tet 2026 campaign, fixed 50K VND off
        new("Khuyến mãi Tết 2026", "TET2026",
            PromotionType.VoucherCode, DiscountType.FixedAmount, 50_000m,
            StartDayOffset: 25, EndDayOffset: 45,
            MinOrderValue: 300_000m, UsageLimitTotal: 500,
            PromotionApplyLevel.Cart, ShouldActivate: false),

        // Draft: VIP 20% off, product-level, no limit
        new("Ưu đãi VIP 20%", "VIP20",
            PromotionType.VoucherCode, DiscountType.Percentage, 20m,
            StartDayOffset: -10, EndDayOffset: 180,
            MinOrderValue: null, UsageLimitTotal: null,
            PromotionApplyLevel.Product, ShouldActivate: false)
    ];
}
