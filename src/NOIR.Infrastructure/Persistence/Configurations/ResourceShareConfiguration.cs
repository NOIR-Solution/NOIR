namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ResourceShare entity.
/// Configures indexes for efficient resource and user lookups.
/// </summary>
public class ResourceShareConfiguration : IEntityTypeConfiguration<ResourceShare>
{
    public void Configure(EntityTypeBuilder<ResourceShare> builder)
    {
        builder.ToTable("ResourceShares");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Resource identification
        builder.Property(e => e.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .IsRequired();

        // User references
        builder.Property(e => e.SharedWithUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(e => e.SharedByUserId)
            .HasMaxLength(450);

        // Permission (stored as string via global convention)
        builder.Property(e => e.Permission)
            .IsRequired();

        // Expiration
        builder.Property(e => e.ExpiresAt);

        // Tenant ID
        builder.Property(e => e.TenantId)
            .HasMaxLength(64);
        builder.HasIndex(e => e.TenantId);

        // Composite index for efficient lookups: "What shares exist for this resource?"
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId, e.TenantId })
            .HasDatabaseName("IX_ResourceShares_Resource");

        // Index for user lookups: "What resources are shared with this user?"
        builder.HasIndex(e => new { e.SharedWithUserId, e.TenantId })
            .HasDatabaseName("IX_ResourceShares_User");

        // Unique constraint: one share per user per resource per tenant
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId, e.SharedWithUserId, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_ResourceShares_Unique");

        // Index for expiration queries
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_ResourceShares_ExpiresAt");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.ModifiedBy).HasMaxLength(450);
        builder.Property(e => e.DeletedBy).HasMaxLength(450);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter (named filter for EF Core 10 compatibility with multi-tenancy)
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Soft delete index
        builder.HasIndex(e => e.IsDeleted);

    }
}
