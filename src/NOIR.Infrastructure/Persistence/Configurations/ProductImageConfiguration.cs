namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductImage entity.
/// </summary>
public class ProductImageConfiguration : TenantEntityConfiguration<ProductImage>
{
    public override void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProductImages");

        builder.Property(e => e.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.AltText)
            .HasMaxLength(200);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsPrimary)
            .HasDefaultValue(false);

        // Relationship
        builder.HasOne(e => e.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for ordering
        builder.HasIndex(e => new { e.ProductId, e.SortOrder })
            .HasDatabaseName("IX_ProductImages_Product_Sort");

        // Filtered index for primary image lookup (TenantId leading for Finbuckle)
        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("IX_ProductImages_TenantId_Primary");
    }
}
