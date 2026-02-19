namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductAttributeValue entity.
/// </summary>
public class ProductAttributeValueConfiguration : TenantEntityConfiguration<ProductAttributeValue>
{
    public override void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProductAttributeValues");

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
            .HasFilter("[IsDeleted] = 0")
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
    }
}
