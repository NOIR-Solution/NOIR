namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PasswordResetOtp entity.
/// </summary>
public class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
{
    public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
    {
        builder.ToTable("PasswordResetOtps");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Email (indexed for rate limiting per email)
        builder.Property(e => e.Email)
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(e => e.Email);

        // User ID (nullable for non-existent emails)
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.HasIndex(e => e.UserId);

        // OTP Hash
        builder.Property(e => e.OtpHash)
            .HasMaxLength(256)
            .IsRequired();

        // Session Token (unique index for lookup)
        builder.Property(e => e.SessionToken)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(e => e.SessionToken).IsUnique();

        // Expiration
        builder.Property(e => e.ExpiresAt).IsRequired();

        // Usage tracking
        builder.Property(e => e.IsUsed).HasDefaultValue(false);
        builder.Property(e => e.AttemptCount).HasDefaultValue(0);
        builder.Property(e => e.ResendCount).HasDefaultValue(0);

        // IP address
        builder.Property(e => e.CreatedByIp).HasMaxLength(45);

        // Reset Token (indexed for lookup after OTP verification)
        builder.Property(e => e.ResetToken).HasMaxLength(128);
        builder.HasIndex(e => e.ResetToken);

        // Filtered index for active OTPs per email (TenantId leading for Finbuckle)
        // Note: SessionToken is globally unique per CLAUDE.md Rule 18, but
        // performance indexes benefit from TenantId as leading column
        builder.HasIndex(e => new { e.TenantId, e.Email, e.ExpiresAt })
            .HasFilter("[IsUsed] = 0 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_PasswordResetOtps_Active");

        // Composite index for session token lookup with usage status
        builder.HasIndex(e => new { e.SessionToken, e.IsUsed });

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

        // Ignored computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsValid);
        builder.Ignore(e => e.IsResetTokenValid);
    }
}
