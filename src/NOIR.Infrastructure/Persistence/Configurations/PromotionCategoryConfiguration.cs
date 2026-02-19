namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PromotionCategory junction entity.
/// </summary>
public class PromotionCategoryConfiguration : TenantEntityConfiguration<Domain.Entities.Promotion.PromotionCategory>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Promotion.PromotionCategory> builder)
    {
        base.Configure(builder);

        builder.ToTable("PromotionCategories");

        // Relationship with Promotion
        builder.HasOne(e => e.Promotion)
            .WithMany(p => p.Categories)
            .HasForeignKey(e => e.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for lookups
        builder.HasIndex(e => new { e.PromotionId, e.CategoryId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_PromotionCategories_PromotionId_CategoryId");

        builder.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_PromotionCategories_CategoryId");
    }
}
