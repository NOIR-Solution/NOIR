namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductOption entity.
/// </summary>
public class ProductOptionConfiguration : TenantEntityConfiguration<ProductOption>
{
    public override void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProductOptions");

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

        // Unique constraint: same option name cannot exist twice for same product per tenant (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.ProductId, e.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_ProductOptions_TenantId_Product_Name");

        // Product lookup index
        builder.HasIndex(e => new { e.ProductId, e.SortOrder })
            .HasDatabaseName("IX_ProductOptions_Product_Sort");
    }
}
