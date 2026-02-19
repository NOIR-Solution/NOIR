namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CategoryAttribute junction entity.
/// </summary>
public class CategoryAttributeConfiguration : TenantEntityConfiguration<CategoryAttribute>
{
    public override void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        base.Configure(builder);

        builder.ToTable("CategoryAttributes");

        // Foreign keys
        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.Property(e => e.AttributeId)
            .IsRequired();

        // Multi-tenant unique constraint: One link per Category-Attribute pair per Tenant (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.CategoryId, e.AttributeId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
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
    }
}
