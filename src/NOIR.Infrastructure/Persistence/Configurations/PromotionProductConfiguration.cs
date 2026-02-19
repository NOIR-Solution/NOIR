namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionProduct junction entity.
/// </summary>
public class PromotionProductConfiguration : TenantEntityConfiguration<Domain.Entities.Promotion.PromotionProduct>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionProduct> builder)
    {
        base.Configure(builder);

        builder.ToTable("PromotionProducts");

        // Relationship with Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Products)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for lookups
        builder.HasIndex(e => new { e.PromotionId, e.ProductId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_PromotionProducts_PromotionId_ProductId");

        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_PromotionProducts_ProductId");
    }
}
