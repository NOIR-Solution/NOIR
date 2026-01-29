namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductAttributeValue entity.
/// </summary>
public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValues");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Identity
        builder.Property(e => e.Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.DisplayValue)
            .HasMaxLength(200)
            .IsRequired();

        // Multi-tenant unique constraint: Value unique within Attribute per Tenant (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.AttributeId, e.Value })
            .IsUnique()
            .HasDatabaseName("IX_ProductAttributeValues_TenantId_AttributeId_Value");

        // Visual display
        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.Property(e => e.SwatchUrl)
            .HasMaxLength(500);

        builder.Property(e => e.IconUrl)
            .HasMaxLength(500);

        // Organization
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Analytics
        builder.Property(e => e.ProductCount)
            .HasDefaultValue(0);

        // Foreign key to ProductAttribute
        builder.Property(e => e.AttributeId)
            .IsRequired();

        // Navigation configured in ProductAttributeConfiguration

        // Indexes for filtering
        builder.HasIndex(e => new { e.TenantId, e.AttributeId, e.IsActive })
            .HasDatabaseName("IX_ProductAttributeValues_TenantId_AttributeId_IsActive");

        builder.HasIndex(e => new { e.TenantId, e.AttributeId, e.SortOrder })
            .HasDatabaseName("IX_ProductAttributeValues_TenantId_AttributeId_SortOrder");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
