namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionProduct junction entity.
/// </summary>
public class PromotionProductConfiguration : IEntityTypeConfiguration<Domain.Entities.Promotion.PromotionProduct>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionProduct> builder)
    {
        builder.ToTable("PromotionProducts");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Relationship with Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Products)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for lookups
        builder.HasIndex(e => new { e.PromotionId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionProducts_PromotionId_ProductId");

        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_PromotionProducts_ProductId");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
