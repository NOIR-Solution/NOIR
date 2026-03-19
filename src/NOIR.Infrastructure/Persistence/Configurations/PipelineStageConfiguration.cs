namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PipelineStage entity.
/// </summary>
public class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.ToTable("PipelineStages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name (unique per pipeline per tenant)
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.Name, e.PipelineId, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_PipelineStages_Name_PipelineId_TenantId");

        // SortOrder
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Color
        builder.Property(e => e.Color)
            .HasMaxLength(7)
            .HasDefaultValue("#6366f1");

        // StageType (stored as string per project enum serialization standard)
        builder.Property(e => e.StageType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(StageType.Active);

        // IsSystem — Won/Lost stages are protected from deletion/rename
        builder.Property(e => e.IsSystem)
            .HasDefaultValue(false);

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
