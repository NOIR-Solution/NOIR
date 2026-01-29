namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CategoryAttribute junction entity.
/// </summary>
public class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        builder.ToTable("CategoryAttributes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.Property(e => e.AttributeId)
            .IsRequired();

        // Multi-tenant unique constraint: One link per Category-Attribute pair per Tenant (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.CategoryId, e.AttributeId })
            .IsUnique()
            .HasDatabaseName("IX_CategoryAttributes_TenantId_CategoryId_AttributeId");

        // Category-specific overrides
        builder.Property(e => e.IsRequired)
            .HasDefaultValue(false);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Navigation to ProductCategory
        // Using Cascade because category-specific attribute configurations are meaningless without the category
        // When a category is deleted, all its attribute assignments are automatically removed
        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to ProductAttribute
        // Using Restrict to prevent accidental deletion when CategoryAttribute links exist
        // Attributes are reusable across categories/products, so deletion requires explicit unassignment first
        // DeleteProductAttributeCommandHandler validates this constraint with user-friendly error messages
        builder.HasOne(e => e.Attribute)
            .WithMany()
            .HasForeignKey(e => e.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for filtering
        builder.HasIndex(e => new { e.TenantId, e.CategoryId, e.SortOrder })
            .HasDatabaseName("IX_CategoryAttributes_TenantId_CategoryId_SortOrder");

        builder.HasIndex(e => new { e.TenantId, e.AttributeId })
            .HasDatabaseName("IX_CategoryAttributes_TenantId_AttributeId");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
