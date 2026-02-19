namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ResourceShare entity.
/// Configures indexes for efficient resource and user lookups.
/// </summary>
public class ResourceShareConfiguration : TenantEntityConfiguration<ResourceShare>
{
    public override void Configure(EntityTypeBuilder<ResourceShare> builder)
    {
        base.Configure(builder);

        builder.ToTable("ResourceShares");

        // Resource identification
        builder.Property(e => e.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .IsRequired();

        // User references
        builder.Property(e => e.SharedWithUserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        builder.Property(e => e.SharedByUserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        // Permission (stored as string via global convention)
        builder.Property(e => e.Permission)
            .IsRequired();

        // Expiration
        builder.Property(e => e.ExpiresAt);

        // Composite index for efficient lookups: "What shares exist for this resource?"
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId, e.TenantId })
            .HasDatabaseName("IX_ResourceShares_Resource");

        // Index for user lookups: "What resources are shared with this user?"
        builder.HasIndex(e => new { e.SharedWithUserId, e.TenantId })
            .HasDatabaseName("IX_ResourceShares_User");

        // Unique constraint: one share per user per resource per tenant
        builder.HasIndex(e => new { e.ResourceType, e.ResourceId, e.SharedWithUserId, e.TenantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_ResourceShares_Unique");

        // Index for expiration queries
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_ResourceShares_ExpiresAt");
    }
}
