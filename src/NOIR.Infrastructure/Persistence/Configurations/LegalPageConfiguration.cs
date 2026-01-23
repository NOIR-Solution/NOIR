namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for LegalPage entity.
/// </summary>
public class LegalPageConfiguration : IEntityTypeConfiguration<LegalPage>
{
    public void Configure(EntityTypeBuilder<LegalPage> builder)
    {
        builder.ToTable("LegalPages");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Slug (URL-friendly identifier)
        builder.Property(e => e.Slug)
            .HasMaxLength(100)
            .IsRequired();

        // Unique constraint on Slug + TenantId (one page per tenant per slug)
        builder.HasIndex(e => new { e.Slug, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_LegalPages_Slug_TenantId");

        // Title
        builder.Property(e => e.Title)
            .HasMaxLength(200)
            .IsRequired();

        // HTML Content (large text - use nvarchar(max))
        builder.Property(e => e.HtmlContent)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // SEO fields
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(60);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(160);

        builder.Property(e => e.CanonicalUrl)
            .HasMaxLength(500);

        builder.Property(e => e.AllowIndexing)
            .HasDefaultValue(true);

        // Status and version
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Version).HasDefaultValue(1);
        builder.Property(e => e.LastModified).IsRequired();

        // Index for active page lookup
        builder.HasIndex(e => new { e.Slug, e.IsActive, e.IsDeleted });

        // Tenant ID (nullable for platform-level pages)
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength)
            .IsRequired(false); // Allow null for platform-level pages
        builder.HasIndex(e => e.TenantId);

        // Filtered index for platform page lookup optimization
        // This index optimizes queries that look up platform defaults (TenantId = null)
        builder.HasIndex(e => new { e.Slug, e.IsActive })
            .HasDatabaseName("IX_LegalPages_Platform_Lookup")
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
