namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailTemplate entity.
/// </summary>
public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name (template identifier)
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Unique constraint on Name + TenantId (one template per tenant)
        builder.HasIndex(e => new { e.Name, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_Name_TenantId");

        // Subject
        builder.Property(e => e.Subject)
            .HasMaxLength(500)
            .IsRequired();

        // HTML Body (large text - use nvarchar(max) for email templates)
        builder.Property(e => e.HtmlBody)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Plain Text Body (optional, large text - use nvarchar(max))
        builder.Property(e => e.PlainTextBody)
            .HasColumnType("nvarchar(max)");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Available Variables (JSON array)
        builder.Property(e => e.AvailableVariables)
            .HasMaxLength(2000);

        // Status and version
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Version).HasDefaultValue(1);

        // Index for active template lookup
        builder.HasIndex(e => new { e.Name, e.IsActive, e.IsDeleted });

        // Tenant ID (nullable for platform-level templates)
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength)
            .IsRequired(false); // Allow null for platform-level templates
        builder.HasIndex(e => e.TenantId);

        // Filtered index for platform template lookup optimization
        // This index optimizes queries that look up platform defaults (TenantId = null)
        // which are the most frequently accessed templates (fallback for all tenants)
        builder.HasIndex(e => new { e.Name, e.IsActive })
            .HasDatabaseName("IX_EmailTemplates_Platform_Lookup")
            .HasFilter("[TenantId] IS NULL AND [IsDeleted] = 0");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
