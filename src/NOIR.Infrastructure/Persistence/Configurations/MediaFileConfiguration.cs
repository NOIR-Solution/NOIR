namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MediaFile entity.
/// </summary>
public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFiles");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // ShortId (8-char unique identifier for quick lookups)
        builder.Property(e => e.ShortId)
            .HasMaxLength(8)
            .IsRequired();

        builder.HasIndex(e => e.ShortId)
            .IsUnique()
            .HasDatabaseName("IX_MediaFiles_ShortId");

        // Slug (unique per tenant, contains shortId after underscore)
        builder.Property(e => e.Slug)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_MediaFiles_TenantId_Slug");

        // Original filename
        builder.Property(e => e.OriginalFileName)
            .HasMaxLength(500)
            .IsRequired();

        // Folder
        builder.Property(e => e.Folder)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Folder })
            .HasDatabaseName("IX_MediaFiles_TenantId_Folder");

        // Default URL
        builder.Property(e => e.DefaultUrl)
            .HasMaxLength(2000)
            .IsRequired();

        // Placeholders
        builder.Property(e => e.ThumbHash)
            .HasMaxLength(100);

        builder.Property(e => e.DominantColor)
            .HasMaxLength(10);

        // Image metadata
        builder.Property(e => e.Width);
        builder.Property(e => e.Height);

        builder.Property(e => e.Format)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SizeBytes);
        builder.Property(e => e.HasTransparency);

        // Ignore computed property
        builder.Ignore(e => e.AspectRatio);

        // JSON columns for variants and srcsets
        builder.Property(e => e.VariantsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.SrcsetsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Alt text
        builder.Property(e => e.AltText)
            .HasMaxLength(500);

        // Uploader
        builder.Property(e => e.UploadedBy)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // TenantId as leading column for Finbuckle query optimization
        builder.HasIndex(e => new { e.TenantId, e.UploadedBy })
            .HasDatabaseName("IX_MediaFiles_TenantId_UploadedBy");

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
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
