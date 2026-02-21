namespace NOIR.Infrastructure.Persistence.Configurations;

public class TenantModuleStateConfiguration : IEntityTypeConfiguration<TenantModuleState>
{
    public void Configure(EntityTypeBuilder<TenantModuleState> builder)
    {
        builder.ToTable("TenantModuleStates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.FeatureName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        // Rule 18: Unique constraints MUST include TenantId
        builder.HasIndex(e => new { e.TenantId, e.FeatureName })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_TenantModuleStates_TenantId_FeatureName");

        // Index for bulk tenant queries
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_TenantModuleStates_TenantId");

        // Audit fields
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
