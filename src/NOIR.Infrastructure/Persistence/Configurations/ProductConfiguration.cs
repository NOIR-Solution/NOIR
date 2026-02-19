namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Product entity.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Basic info
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_Products_TenantId_Slug");

        builder.Property(e => e.ShortDescription)
            .HasMaxLength(300);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.DescriptionHtml);

        // Pricing
        builder.Property(e => e.BasePrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        // Physical Properties
        builder.Property(e => e.Weight)
            .HasPrecision(18, 4);

        builder.Property(e => e.WeightUnit)
            .HasMaxLength(10);

        builder.Property(e => e.Length)
            .HasPrecision(18, 4);

        builder.Property(e => e.Width)
            .HasPrecision(18, 4);

        builder.Property(e => e.Height)
            .HasPrecision(18, 4);

        builder.Property(e => e.DimensionUnit)
            .HasMaxLength(10);

        // Status
        builder.Property(e => e.Status)
            .HasConversion<int>();

        // Organization
        builder.HasOne(e => e.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Brand)
            .HasMaxLength(100);

        // Brand entity relationship (new - replaces string Brand)
        builder.HasOne(e => e.BrandEntity)
            .WithMany(b => b.Products)
            .HasForeignKey(e => e.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.BrandId })
            .HasDatabaseName("IX_Products_TenantId_BrandId");

        // Identification
        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.Sku })
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL")
            .HasDatabaseName("IX_Products_TenantId_Sku");

        builder.Property(e => e.Barcode)
            .HasMaxLength(50);

        // SEO
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(100);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(300);

        // Indexes for filtering
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CategoryId })
            .HasDatabaseName("IX_Products_TenantId_Status_Category");

        builder.HasIndex(e => new { e.TenantId, e.BasePrice })
            .HasDatabaseName("IX_Products_TenantId_Price");

        builder.HasIndex(e => new { e.TenantId, e.Brand })
            .HasDatabaseName("IX_Products_TenantId_Brand");

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

        // Ignore computed properties
        builder.Ignore(e => e.HasVariants);
        builder.Ignore(e => e.HasOptions);
        builder.Ignore(e => e.TotalStock);
        builder.Ignore(e => e.InStock);
        builder.Ignore(e => e.PrimaryImage);
    }
}
