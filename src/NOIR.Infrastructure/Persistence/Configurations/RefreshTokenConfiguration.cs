namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Token (unique index for lookup)
        builder.Property(e => e.Token)
            .HasMaxLength(128)
            .IsRequired();
        builder.HasIndex(e => e.Token).IsUnique();

        // User ID
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();
        builder.HasIndex(e => e.UserId);

        // Token Family (for theft detection)
        builder.HasIndex(e => e.TokenFamily);

        // Composite index for active tokens query
        builder.HasIndex(e => new { e.UserId, e.IsDeleted });

        // Expiration
        builder.Property(e => e.ExpiresAt).IsRequired();

        // IP addresses
        builder.Property(e => e.CreatedByIp).HasMaxLength(45);
        builder.Property(e => e.RevokedByIp).HasMaxLength(45);

        // Device info
        builder.Property(e => e.DeviceFingerprint).HasMaxLength(256);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.DeviceName).HasMaxLength(200);

        // Revocation
        builder.Property(e => e.ReasonRevoked).HasMaxLength(500);
        builder.Property(e => e.ReplacedByToken).HasMaxLength(128);

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter (named filter for EF Core 10 compatibility with multi-tenancy)
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Ignored computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsRevoked);
        builder.Ignore(e => e.IsActive);
    }
}
