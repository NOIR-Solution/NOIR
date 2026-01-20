namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for HandlerAuditLog entity.
/// Middle level of the audit hierarchy - captures handler/command execution with DTO diff.
/// </summary>
public class HandlerAuditLogConfiguration : IEntityTypeConfiguration<HandlerAuditLog>
{
    public void Configure(EntityTypeBuilder<HandlerAuditLog> builder)
    {
        builder.ToTable("HandlerAuditLogs");

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

        // Handler info
        builder.Property(e => e.HandlerName)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(e => e.HandlerName);

        builder.Property(e => e.OperationType)
            .HasMaxLength(20)
            .IsRequired();

        // Activity Context - for timeline display
        builder.Property(e => e.PageContext).HasMaxLength(50);
        builder.HasIndex(e => e.PageContext);

        builder.Property(e => e.ActionDescription).HasMaxLength(500);
        builder.Property(e => e.TargetDisplayName).HasMaxLength(200);

        // Target DTO
        builder.Property(e => e.TargetDtoType).HasMaxLength(200);
        builder.Property(e => e.TargetDtoId).HasMaxLength(100);
        builder.HasIndex(e => new { e.TargetDtoType, e.TargetDtoId });

        // Diff (RFC 6902 JSON Patch)
        builder.Property(e => e.DtoDiff).HasColumnType("nvarchar(max)");

        // Input/Output (JSON, sanitized)
        builder.Property(e => e.InputParameters).HasColumnType("nvarchar(max)");
        builder.Property(e => e.OutputResult).HasColumnType("nvarchar(max)");

        // Timing & Status
        builder.Property(e => e.StartTime).IsRequired();
        builder.Property(e => e.IsSuccess).HasDefaultValue(true);
        builder.Property(e => e.ErrorMessage).HasColumnType("nvarchar(max)");

        // Archiving - for retention policy
        builder.Property(e => e.IsArchived).HasDefaultValue(false);
        builder.HasIndex(e => e.IsArchived);
        builder.HasIndex(e => new { e.IsArchived, e.ArchivedAt });

        // Navigation
        builder.HasMany(e => e.EntityAuditLogs)
            .WithOne(ea => ea.HandlerAuditLog)
            .HasForeignKey(ea => ea.HandlerAuditLogId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: HandlerAuditLog does NOT have soft delete - we never delete audit logs
    }
}
