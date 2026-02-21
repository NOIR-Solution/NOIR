namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds engagement data: promotions and voucher campaigns.
/// Order: 400 (runs after Commerce at 300).
/// </summary>
public class EngagementSeedModule : ISeedDataModule
{
    public int Order => 400;
    public string ModuleName => "Engagement";

    public async Task SeedAsync(SeedDataContext context, CancellationToken ct = default)
    {
        var tenantId = context.CurrentTenant.Id;

        // Idempotency: skip if promotions already exist for this tenant
        var hasData = await context.DbContext.Set<Domain.Entities.Promotion.Promotion>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:CheckEngagement")
            .AnyAsync(p => p.TenantId == tenantId, ct);

        if (hasData)
        {
            context.Logger.LogInformation("[SeedData] Engagement already seeded for {Tenant}", tenantId);
            return;
        }

        var promotionDefs = EngagementData.GetPromotions();

        foreach (var def in promotionDefs)
        {
            var startDate = SeedDataConstants.SpreadDate(def.StartDayOffset);
            var endDate = SeedDataConstants.SpreadDate(def.EndDayOffset);

            var promoId = SeedDataConstants.TenantGuid(tenantId, $"promo:{def.Code}");
            var promo = Domain.Entities.Promotion.Promotion.Create(
                name: def.Name,
                code: def.Code,
                promotionType: def.Type,
                discountType: def.DiscountType,
                discountValue: def.DiscountValue,
                startDate: startDate,
                endDate: endDate,
                applyLevel: def.ApplyLevel,
                minOrderValue: def.MinOrderValue,
                usageLimitTotal: def.UsageLimitTotal,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(promo, promoId);

            if (def.ShouldActivate)
            {
                promo.Activate();
            }

            context.DbContext.Set<Domain.Entities.Promotion.Promotion>().Add(promo);
        }

        await context.DbContext.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[SeedData] Engagement: {Count} promotions",
            promotionDefs.Length);
    }
}
