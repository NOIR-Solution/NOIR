namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PostTag entity.
/// </summary>
public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.ToTable("PostTags");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Slug (unique per tenant)
        builder.Property(e => e.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.Slug, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_PostTags_Slug_TenantId");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Color
        builder.Property(e => e.Color)
            .HasMaxLength(20);

        // Post count
        builder.Property(e => e.PostCount)
            .HasDefaultValue(0);

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

/// <summary>
/// EF Core configuration for PostTagAssignment join entity.
/// </summary>
public class PostTagAssignmentConfiguration : IEntityTypeConfiguration<PostTagAssignment>
{
    public void Configure(EntityTypeBuilder<PostTagAssignment> builder)
    {
        builder.ToTable("PostTagAssignments");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Unique constraint (one tag per post)
        builder.HasIndex(e => new { e.PostId, e.TagId })
            .IsUnique()
            .HasDatabaseName("IX_PostTagAssignments_PostId_TagId");

        // Post relationship
        builder.HasOne(e => e.Post)
            .WithMany(p => p.TagAssignments)
            .HasForeignKey(e => e.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tag relationship
        builder.HasOne(e => e.Tag)
            .WithMany(t => t.PostAssignments)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);
    }
}
