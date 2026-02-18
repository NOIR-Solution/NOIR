namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductReview entity.
/// </summary>
public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        builder.Property(e => e.Rating)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(200);

        builder.Property(e => e.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.AdminResponse)
            .HasMaxLength(2000);

        // Unique constraint: one review per user per product per tenant
        builder.HasIndex(e => new { e.TenantId, e.ProductId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ProductReviews_TenantId_ProductId_UserId");

        // Index for fast product review queries (TenantId leading for Finbuckle)
        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasDatabaseName("IX_ProductReviews_TenantId_ProductId");

        // Index for user review queries
        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasDatabaseName("IX_ProductReviews_TenantId_UserId");

        // Index for moderation queue
        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_ProductReviews_TenantId_Status");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_ProductReviews_TenantId");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
