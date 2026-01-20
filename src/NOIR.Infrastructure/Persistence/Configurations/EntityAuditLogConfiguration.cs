namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EntityAuditLog entity.
/// Bottom level of the audit hierarchy - captures entity-level changes with property diff.
/// </summary>
public class EntityAuditLogConfiguration : IEntityTypeConfiguration<EntityAuditLog>
{
    public void Configure(EntityTypeBuilder<EntityAuditLog> builder)
    {
        builder.ToTable("EntityAuditLogs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Correlation ID
        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(e => e.CorrelationId);

        // Tenant
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Entity info
        builder.Property(e => e.EntityType)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(e => e.EntityId)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        // Composite index for entity history queries (ordered by time)
        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp })
            .HasDatabaseName("IX_EntityAuditLogs_EntityHistory");

        builder.Property(e => e.Operation)
            .HasMaxLength(20)
            .IsRequired();

        // Diff (RFC 6902 JSON Patch extended with oldValue)
        builder.Property(e => e.EntityDiff).HasColumnType("nvarchar(max)");

        // Ordering
        builder.Property(e => e.Timestamp).IsRequired();
        builder.HasIndex(e => e.Timestamp);
        builder.Property(e => e.Version).HasDefaultValue(1);

        // Archiving - for retention policy
        builder.Property(e => e.IsArchived).HasDefaultValue(false);
        builder.HasIndex(e => e.IsArchived);
        builder.HasIndex(e => new { e.IsArchived, e.ArchivedAt });

        // Note: EntityAuditLog does NOT have soft delete - we never delete audit logs
    }
}
