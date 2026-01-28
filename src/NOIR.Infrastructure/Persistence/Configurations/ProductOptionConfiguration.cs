namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductOption entity.
/// </summary>
public class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("ProductOptions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(100);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Relationship to Product
        builder.HasOne(e => e.Product)
            .WithMany(p => p.Options)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: same option name cannot exist twice for same product
        builder.HasIndex(e => new { e.ProductId, e.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProductOptions_Product_Name");

        // Product lookup index
        builder.HasIndex(e => new { e.ProductId, e.SortOrder })
            .HasDatabaseName("IX_ProductOptions_Product_Sort");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);
    }
}
