namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductCategory entity.
/// </summary>
public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Slug (unique per tenant - CLAUDE.md Rule 19)
        builder.Property(e => e.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_ProductCategories_TenantId_Slug");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Image
        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        // SEO
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(100);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(300);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Product count
        builder.Property(e => e.ProductCount)
            .HasDefaultValue(0);

        // Self-referencing hierarchy
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.ParentId, e.SortOrder })
            .HasDatabaseName("IX_ProductCategories_TenantId_Parent_Sort");

        // Tenant ID
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
