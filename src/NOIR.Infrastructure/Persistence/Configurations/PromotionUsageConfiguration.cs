namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionUsage entity.
/// </summary>
public class PromotionUsageConfiguration : TenantEntityConfiguration<Domain.Entities.Promotion.PromotionUsage>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionUsage> builder)
    {
        base.Configure(builder);

        builder.ToTable("PromotionUsages");

        // UserId
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // DiscountAmount
        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2);

        // Relationship with Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Usages)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => new { e.PromotionId, e.UserId })
            .HasDatabaseName("IX_PromotionUsages_PromotionId_UserId");

        builder.HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_PromotionUsages_OrderId");

        builder.HasIndex(e => new { e.TenantId, e.UsedAt })
            .HasDatabaseName("IX_PromotionUsages_TenantId_UsedAt");
    }
}
