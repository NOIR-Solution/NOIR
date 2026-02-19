namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductOptionValue entity.
/// </summary>
public class ProductOptionValueConfiguration : TenantEntityConfiguration<ProductOptionValue>
{
    public override void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        base.Configure(builder);

        builder.ToTable("ProductOptionValues");

        builder.Property(e => e.Value)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DisplayValue)
            .HasMaxLength(100);

        builder.Property(e => e.ColorCode)
            .HasMaxLength(20);

        builder.Property(e => e.SwatchUrl)
            .HasMaxLength(500);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Relationship to ProductOption
        builder.HasOne(e => e.Option)
            .WithMany(o => o.Values)
            .HasForeignKey(e => e.OptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: same value cannot exist twice for same option per tenant (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.OptionId, e.Value })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_ProductOptionValues_TenantId_Option_Value");

        // Option lookup index
        builder.HasIndex(e => new { e.OptionId, e.SortOrder })
            .HasDatabaseName("IX_ProductOptionValues_Option_Sort");
    }
}
