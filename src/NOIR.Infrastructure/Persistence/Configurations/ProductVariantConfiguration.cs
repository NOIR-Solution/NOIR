namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductVariant entity.
/// </summary>
public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.Sku })
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL")
            .HasDatabaseName("IX_ProductVariants_TenantId_Sku");

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.CompareAtPrice)
            .HasPrecision(18, 2);

        // Concurrency token for stock updates
        builder.Property(e => e.StockQuantity)
            .IsConcurrencyToken();

        builder.Property(e => e.OptionsJson);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Relationship to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional relationship to ProductImage
        // Note: Using NoAction to avoid SQL Server cascade path conflicts
        builder.HasOne(e => e.Image)
            .WithMany()
            .HasForeignKey(e => e.ImageId)
            .OnDelete(DeleteBehavior.NoAction);

        // Stock filtering index
        builder.HasIndex(e => new { e.TenantId, e.StockQuantity })
            .HasFilter("[StockQuantity] > 0")
            .HasDatabaseName("IX_ProductVariants_TenantId_InStock");

        // Product lookup index
        builder.HasIndex(e => new { e.ProductId, e.SortOrder })
            .HasDatabaseName("IX_ProductVariants_Product_Sort");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);

        // Ignore computed properties
        builder.Ignore(e => e.InStock);
        builder.Ignore(e => e.LowStock);
        builder.Ignore(e => e.OnSale);
    }
}
