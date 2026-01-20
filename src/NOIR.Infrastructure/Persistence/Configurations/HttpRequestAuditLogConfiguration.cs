namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for HttpRequestAuditLog entity.
/// Top level of the audit hierarchy - captures HTTP request/response context.
/// </summary>
public class HttpRequestAuditLogConfiguration : IEntityTypeConfiguration<HttpRequestAuditLog>
{
    public void Configure(EntityTypeBuilder<HttpRequestAuditLog> builder)
    {
        builder.ToTable("HttpRequestAuditLogs");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Correlation ID - unique per request
        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(e => e.CorrelationId).IsUnique();

        // User context
        builder.Property(e => e.UserId).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.HasIndex(e => e.UserId);
        builder.Property(e => e.UserEmail).HasMaxLength(256);

        // Tenant
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Composite index for tenant-scoped time-based queries (common in multi-tenant scenarios)
        builder.HasIndex(e => new { e.TenantId, e.StartTime })
            .HasDatabaseName("IX_HttpRequestAuditLogs_Tenant_StartTime");

        // Request info
        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(e => e.Url)
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(e => e.QueryString).HasMaxLength(2000);
        builder.Property(e => e.RequestHeaders).HasColumnType("nvarchar(max)");
        builder.Property(e => e.RequestBody).HasColumnType("nvarchar(max)");

        // Response info
        builder.Property(e => e.ResponseBody).HasColumnType("nvarchar(max)");

        // Context
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        // Timing
        builder.Property(e => e.StartTime).IsRequired();
        builder.HasIndex(e => e.StartTime);

        // Archiving - for retention policy
        builder.Property(e => e.IsArchived).HasDefaultValue(false);
        builder.HasIndex(e => e.IsArchived);
        builder.HasIndex(e => new { e.IsArchived, e.ArchivedAt });

        // Navigation
        builder.HasMany(e => e.HandlerAuditLogs)
            .WithOne(h => h.HttpRequestAuditLog)
            .HasForeignKey(h => h.HttpRequestAuditLogId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: HttpRequestAuditLog does NOT have soft delete - we never delete audit logs
    }
}
