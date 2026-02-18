namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ReviewMedia entity.
/// </summary>
public class ReviewMediaConfiguration : IEntityTypeConfiguration<ReviewMedia>
{
    public void Configure(EntityTypeBuilder<ReviewMedia> builder)
    {
        builder.ToTable("ReviewMedia");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.MediaUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.MediaType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DisplayOrder)
            .HasDefaultValue(0);

        // Relationship
        builder.HasOne(e => e.Review)
            .WithMany(r => r.Media)
            .HasForeignKey(e => e.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for ordering
        builder.HasIndex(e => new { e.ReviewId, e.DisplayOrder })
            .HasDatabaseName("IX_ReviewMedia_Review_Sort");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_ReviewMedia_TenantId");
    }
}
