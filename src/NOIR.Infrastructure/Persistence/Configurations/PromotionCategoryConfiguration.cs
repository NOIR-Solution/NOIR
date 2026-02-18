namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionCategory junction entity.
/// </summary>
public class PromotionCategoryConfiguration : IEntityTypeConfiguration<Domain.Entities.Promotion.PromotionCategory>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionCategory> builder)
    {
        builder.ToTable("PromotionCategories");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Relationship with Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Categories)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for lookups
        builder.HasIndex(e => new { e.PromotionId, e.CategoryId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionCategories_PromotionId_CategoryId");

        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_PromotionCategories_CategoryId");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
