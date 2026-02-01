namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductAttribute entity.
/// </summary>
public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Identity
        builder.Property(e => e.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Multi-tenant unique constraint (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_ProductAttributes_TenantId_Code");

        // Behavior flags
        builder.Property(e => e.IsFilterable)
            .HasDefaultValue(false);

        builder.Property(e => e.IsSearchable)
            .HasDefaultValue(false);

        builder.Property(e => e.IsRequired)
            .HasDefaultValue(false);

        builder.Property(e => e.IsVariantAttribute)
            .HasDefaultValue(false);

        builder.Property(e => e.ShowInProductCard)
            .HasDefaultValue(false);

        builder.Property(e => e.ShowInSpecifications)
            .HasDefaultValue(true);

        // Type-specific configuration
        builder.Property(e => e.Unit)
            .HasMaxLength(50);

        builder.Property(e => e.ValidationRegex)
            .HasMaxLength(500);

        builder.Property(e => e.MinValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.MaxValue)
            .HasPrecision(18, 4);

        builder.Property(e => e.DefaultValue)
            .HasMaxLength(500);

        builder.Property(e => e.Placeholder)
            .HasMaxLength(200);

        builder.Property(e => e.HelpText)
            .HasMaxLength(500);

        // Organization
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.IsGlobal)
            .HasDefaultValue(false);

        // Values collection
        builder.HasMany(e => e.Values)
            .WithOne(v => v.Attribute)
            .HasForeignKey(v => v.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for filtering (TenantId as leading column for Finbuckle)
        builder.HasIndex(e => new { e.TenantId, e.Type })
            .HasDatabaseName("IX_ProductAttributes_TenantId_Type");

        builder.HasIndex(e => new { e.TenantId, e.SortOrder, e.Name })
            .HasDatabaseName("IX_ProductAttributes_TenantId_SortOrder_Name");

        // Filtered indexes for sparse boolean columns (TenantId leading)
        builder.HasIndex(e => new { e.TenantId, e.SortOrder })
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_ProductAttributes_TenantId_Active");

        builder.HasIndex(e => new { e.TenantId, e.SortOrder })
            .HasFilter("[IsFilterable] = 1 AND [IsActive] = 1")
            .HasDatabaseName("IX_ProductAttributes_TenantId_Filterable");

        builder.HasIndex(e => new { e.TenantId, e.SortOrder })
            .HasFilter("[IsVariantAttribute] = 1 AND [IsActive] = 1")
            .HasDatabaseName("IX_ProductAttributes_TenantId_Variant");

        builder.HasIndex(e => new { e.TenantId, e.SortOrder })
            .HasFilter("[IsGlobal] = 1 AND [IsActive] = 1")
            .HasDatabaseName("IX_ProductAttributes_TenantId_Global");

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
    }
}
