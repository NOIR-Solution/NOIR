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

        // Language
        builder.Property(e => e.Language)
            .HasMaxLength(10)
            .IsRequired();

        // Unique constraint on Name + Language + TenantId (one template per language per tenant)
        builder.HasIndex(e => new { e.Name, e.Language, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_Name_Language_TenantId");

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
        builder.HasIndex(e => new { e.Name, e.Language, e.IsActive, e.IsDeleted });

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(64);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.ModifiedBy).HasMaxLength(450);
        builder.Property(e => e.DeletedBy).HasMaxLength(450);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
