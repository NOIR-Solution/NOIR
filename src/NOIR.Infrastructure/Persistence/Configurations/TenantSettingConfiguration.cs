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
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);

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
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_TenantSettings_TenantId_Key");

        // Index for platform defaults lookup
        builder.HasIndex(e => e.Key)
            .HasDatabaseName("IX_TenantSettings_Key");

        // Index for category-based lookups
        builder.HasIndex(e => new { e.TenantId, e.Category })
            .HasDatabaseName("IX_TenantSettings_TenantId_Category");

        // Filtered index for platform defaults lookup optimization
        // Most lookups query platform defaults (TenantId = null) as fallback
        builder.HasIndex(e => new { e.Key, e.Category })
            .HasDatabaseName("IX_TenantSettings_Platform_Lookup")
            .HasFilter("[TenantId] IS NULL AND [IsDeleted] = 0");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter only (NO tenant query filter - need both NULL and tenant rows)
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Soft delete index
        builder.HasIndex(e => e.IsDeleted);
    }
}
