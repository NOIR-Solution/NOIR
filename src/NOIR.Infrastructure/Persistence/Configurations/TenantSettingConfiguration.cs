namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TenantSetting entity.
/// Configures settings with platform defaults (TenantId = NULL) and tenant overrides.
/// </summary>
public class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.ToTable("TenantSettings");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Tenant ID (nullable - NULL = platform default)
        builder.Property(e => e.TenantId)
            .HasMaxLength(36);

        // Setting key
        builder.Property(e => e.Key)
            .HasMaxLength(100)
            .IsRequired();

        // Setting value
        builder.Property(e => e.Value)
            .HasMaxLength(4000)
            .IsRequired();

        // Metadata
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.DataType).HasMaxLength(20).HasDefaultValue("string");
        builder.Property(e => e.Category).HasMaxLength(50);

        // Unique constraint: one key per tenant (NULL counts as a unique tenant)
        builder.HasIndex(e => new { e.TenantId, e.Key })
            .IsUnique()
            .HasDatabaseName("IX_TenantSettings_TenantId_Key");

        // Index for platform defaults lookup
        builder.HasIndex(e => e.Key)
            .HasDatabaseName("IX_TenantSettings_Key");

        // Index for category-based lookups
        builder.HasIndex(e => new { e.TenantId, e.Category })
            .HasDatabaseName("IX_TenantSettings_TenantId_Category");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.ModifiedBy).HasMaxLength(450);
        builder.Property(e => e.DeletedBy).HasMaxLength(450);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter only (NO tenant query filter - need both NULL and tenant rows)
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Soft delete index
        builder.HasIndex(e => e.IsDeleted);
    }
}
