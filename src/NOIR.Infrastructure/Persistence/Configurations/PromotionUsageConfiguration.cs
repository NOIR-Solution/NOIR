namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionUsage entity.
/// </summary>
public class PromotionUsageConfiguration : IEntityTypeConfiguration<Domain.Entities.Promotion.PromotionUsage>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionUsage> builder)
    {
        builder.ToTable("PromotionUsages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

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

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
