namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Post entity.
/// </summary>
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Title
        builder.Property(e => e.Title)
            .HasMaxLength(500)
            .IsRequired();

        // Slug (unique per tenant)
        builder.Property(e => e.Slug)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(e => new { e.Slug, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_Posts_Slug_TenantId");

        // Excerpt
        builder.Property(e => e.Excerpt)
            .HasMaxLength(1000);

        // Content (large text)
        builder.Property(e => e.ContentJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ContentHtml)
            .HasColumnType("nvarchar(max)");

        // Featured image (URL for backward compatibility)
        builder.Property(e => e.FeaturedImageUrl)
            .HasMaxLength(2000);

        builder.Property(e => e.FeaturedImageAlt)
            .HasMaxLength(500);

        // Featured image MediaFile relationship
        builder.HasOne(e => e.FeaturedImage)
            .WithMany()
            .HasForeignKey(e => e.FeaturedImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.FeaturedImageId)
            .HasDatabaseName("IX_Posts_FeaturedImageId");

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(PostStatus.Draft);

        // SEO fields
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(200);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(500);

        builder.Property(e => e.CanonicalUrl)
            .HasMaxLength(2000);

        builder.Property(e => e.AllowIndexing)
            .HasDefaultValue(true);

        // Statistics
        builder.Property(e => e.ViewCount)
            .HasDefaultValue(0L);

        builder.Property(e => e.ReadingTimeMinutes)
            .HasDefaultValue(1);

        // Category relationship
        builder.HasOne(e => e.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Author (no FK to avoid coupling to Identity)
        builder.Property(e => e.AuthorId)
            .IsRequired();

        builder.HasIndex(e => e.AuthorId)
            .HasDatabaseName("IX_Posts_AuthorId");

        // Indexes for common queries
        builder.HasIndex(e => new { e.Status, e.PublishedAt, e.IsDeleted })
            .HasDatabaseName("IX_Posts_Status_PublishedAt");

        builder.HasIndex(e => new { e.CategoryId, e.Status, e.IsDeleted })
            .HasDatabaseName("IX_Posts_Category_Status");

        builder.HasIndex(e => new { e.ScheduledPublishAt, e.Status })
            .HasDatabaseName("IX_Posts_ScheduledPublish");

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
