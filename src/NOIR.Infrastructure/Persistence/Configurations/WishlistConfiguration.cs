namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Wishlist entity.
/// </summary>
public class WishlistConfiguration : IEntityTypeConfiguration<Domain.Entities.Wishlist.Wishlist>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Wishlist.Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // User association
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Flags
        builder.Property(e => e.IsDefault)
            .HasDefaultValue(false);

        builder.Property(e => e.IsPublic)
            .HasDefaultValue(false);

        // Share token
        builder.Property(e => e.ShareToken)
            .HasMaxLength(128);

        // Index for user lookup
        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasDatabaseName("IX_Wishlists_TenantId_UserId");

        // Unique share token (globally unique for security)
        builder.HasIndex(e => new { e.TenantId, e.ShareToken })
            .IsUnique()
            .HasFilter("[ShareToken] IS NOT NULL")
            .HasDatabaseName("IX_Wishlists_TenantId_ShareToken");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Ignore computed properties
        builder.Ignore(e => e.ItemCount);
    }
}
