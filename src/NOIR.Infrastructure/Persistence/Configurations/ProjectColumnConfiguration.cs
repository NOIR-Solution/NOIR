namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProjectColumn entity.
/// </summary>
public class ProjectColumnConfiguration : IEntityTypeConfiguration<ProjectColumn>
{
    public void Configure(EntityTypeBuilder<ProjectColumn> builder)
    {
        builder.ToTable("ProjectColumns");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // SortOrder
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Color (hex)
        builder.Property(e => e.Color)
            .HasMaxLength(7);

        // StatusMapping (JSON)
        builder.Property(e => e.StatusMapping)
            .HasMaxLength(500);

        // Index on ProjectId + SortOrder
        builder.HasIndex(e => new { e.ProjectId, e.SortOrder })
            .HasDatabaseName("IX_ProjectColumns_ProjectId_SortOrder");

        // Project FK (Cascade — delete columns when project is deleted)
        builder.HasOne(e => e.Project)
            .WithMany(p => p.Columns)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on TenantId + ProjectId + Name
        builder.HasIndex(e => new { e.TenantId, e.ProjectId, e.Name })
            .IsUnique()
            .HasDatabaseName("IX_ProjectColumns_TenantId_ProjectId_Name");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
    }
}
