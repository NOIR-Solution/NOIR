namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for AuditRetentionPolicy entity.
/// Supports tenant-specific and system-wide retention policies.
/// </summary>
public class AuditRetentionPolicyConfiguration : IEntityTypeConfiguration<AuditRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<AuditRetentionPolicy> builder)
    {
        builder.ToTable("AuditRetentionPolicies");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Tenant (null = system-wide default)
        builder.Property(e => e.TenantId).HasMaxLength(64);
        builder.HasIndex(e => e.TenantId);

        // Policy info
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Retention periods
        builder.Property(e => e.HotStorageDays).HasDefaultValue(30);
        builder.Property(e => e.WarmStorageDays).HasDefaultValue(90);
        builder.Property(e => e.ColdStorageDays).HasDefaultValue(365);
        builder.Property(e => e.DeleteAfterDays).HasDefaultValue(2555);

        // Scope
        builder.Property(e => e.EntityTypesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.CompliancePreset)
            .HasMaxLength(50);

        // Options
        builder.Property(e => e.ExportBeforeArchive).HasDefaultValue(true);
        builder.Property(e => e.ExportBeforeDelete).HasDefaultValue(true);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Priority).HasDefaultValue(0);

        // Indexes for efficient policy lookup
        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_AuditRetentionPolicies_TenantActive");

        builder.HasIndex(e => new { e.IsActive, e.Priority })
            .HasDatabaseName("IX_AuditRetentionPolicies_ActivePriority");

        // Unique constraint: one active policy per tenant per compliance preset
        builder.HasIndex(e => new { e.TenantId, e.CompliancePreset, e.IsActive })
            .HasDatabaseName("IX_AuditRetentionPolicies_UniqueActivePreset")
            .HasFilter("[IsActive] = 1")
            .IsUnique();
    }
}
