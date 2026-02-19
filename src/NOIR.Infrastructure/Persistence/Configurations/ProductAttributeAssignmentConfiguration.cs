namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductAttributeAssignment junction entity.
/// Stores a product's actual attribute values with polymorphic value storage.
/// </summary>
public class ProductAttributeAssignmentConfiguration : TenantEntityConfiguration<ProductAttributeAssignment>
{
    public override void Configure(EntityTypeBuilder<ProductAttributeAssignment> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProductAttributeAssignments");

        // Foreign keys
        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.AttributeId)
            .IsRequired();

        builder.Property(e => e.VariantId);

        // Multi-tenant unique constraint: One assignment per Product-Attribute-Variant tuple per Tenant (CLAUDE.md Rule 18)
        // Note: VariantId can be null for product-level attributes, so we need a filtered index for non-variant
        builder.HasIndex(e => new { e.TenantId, e.ProductId, e.AttributeId, e.VariantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId");

        // Polymorphic value storage

        // Select value (single)
        builder.Property(e => e.AttributeValueId);

        // MultiSelect values (JSON array)
        builder.Property(e => e.AttributeValueIds)
            .HasMaxLength(4000)
            .HasColumnType("nvarchar(4000)");

        // Text values
        builder.Property(e => e.TextValue)
            .HasMaxLength(4000)
            .HasColumnType("nvarchar(4000)");

        // Number values
        builder.Property(e => e.NumberValue)
            .HasColumnType("decimal(18,4)");

        // Boolean value
        builder.Property(e => e.BoolValue);

        // Date values
        builder.Property(e => e.DateValue)
            .HasColumnType("date");

        builder.Property(e => e.DateTimeValue)
            .HasColumnType("datetime2");

        // Color value
        builder.Property(e => e.ColorValue)
            .HasMaxLength(20);

        // Range values
        builder.Property(e => e.MinRangeValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(e => e.MaxRangeValue)
            .HasColumnType("decimal(18,4)");

        // File URL
        builder.Property(e => e.FileUrl)
            .HasMaxLength(500);

        // Display value (for search/filtering)
        builder.Property(e => e.DisplayValue)
            .HasMaxLength(500);

        // Navigation to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.AttributeAssignments)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to ProductAttribute
        // Using Restrict to avoid multiple cascade paths (SQL Server limitation)
        // Attributes with assignments cannot be deleted directly
        builder.HasOne(e => e.Attribute)
            .WithMany()
            .HasForeignKey(e => e.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to ProductAttributeValue (for Select type)
        builder.HasOne(e => e.SelectedValue)
            .WithMany()
            .HasForeignKey(e => e.AttributeValueId)
            .OnDelete(DeleteBehavior.SetNull);

        // Navigation to ProductVariant (optional, for variant-specific attributes)
        // Using Restrict to avoid multiple cascade paths (SQL Server limitation)
        // Product → ProductVariant → ProductAttributeAssignment would conflict with
        // Product → ProductAttributeAssignment cascade path
        builder.HasOne(e => e.Variant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for filtering and querying
        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasDatabaseName("IX_ProductAttributeAssignments_TenantId_ProductId");

        builder.HasIndex(e => new { e.TenantId, e.AttributeId })
            .HasDatabaseName("IX_ProductAttributeAssignments_TenantId_AttributeId");

        builder.HasIndex(e => new { e.TenantId, e.AttributeValueId })
            .HasDatabaseName("IX_ProductAttributeAssignments_TenantId_AttributeValueId");
    }
}
