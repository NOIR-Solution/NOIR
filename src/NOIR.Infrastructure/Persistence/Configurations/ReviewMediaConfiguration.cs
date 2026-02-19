namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ReviewMedia entity.
/// </summary>
public class ReviewMediaConfiguration : TenantEntityConfiguration<ReviewMedia>
{
    public override void Configure(EntityTypeBuilder<ReviewMedia> builder)
    {
        base.Configure(builder);

        builder.ToTable("ReviewMedia");

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
    }
}
