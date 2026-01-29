namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Brand entity.
/// </summary>
public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Identity
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        // Multi-tenant unique constraint (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_Brands_TenantId_Slug");

        // Branding
        builder.Property(e => e.LogoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.BannerUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasColumnType("text");

        builder.Property(e => e.Website)
            .HasMaxLength(500);

        // SEO
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(200);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(500);

        // Organization
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Cached metrics
        builder.Property(e => e.ProductCount)
            .HasDefaultValue(0);

        // Indexes for filtering
        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_Brands_TenantId_IsActive");

        builder.HasIndex(e => new { e.TenantId, e.IsFeatured })
            .HasDatabaseName("IX_Brands_TenantId_IsFeatured");

        builder.HasIndex(e => new { e.TenantId, e.SortOrder, e.Name })
            .HasDatabaseName("IX_Brands_TenantId_SortOrder_Name");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
