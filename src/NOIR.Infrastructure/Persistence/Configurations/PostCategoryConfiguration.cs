namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PostCategory entity.
/// </summary>
public class PostCategoryConfiguration : IEntityTypeConfiguration<PostCategory>
{
    public void Configure(EntityTypeBuilder<PostCategory> builder)
    {
        builder.ToTable("PostCategories");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Slug (unique per tenant)
        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_PostCategories_TenantId_Slug");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // SEO fields
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(200);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(500);

        // Image
        builder.Property(e => e.ImageUrl)
            .HasMaxLength(2000);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Post count
        builder.Property(e => e.PostCount)
            .HasDefaultValue(0);

        // Self-referencing hierarchy (parent-child)
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes (TenantId first for Finbuckle query optimization)
        builder.HasIndex(e => new { e.TenantId, e.ParentId, e.SortOrder })
            .HasDatabaseName("IX_PostCategories_TenantId_Parent_Sort");

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
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
