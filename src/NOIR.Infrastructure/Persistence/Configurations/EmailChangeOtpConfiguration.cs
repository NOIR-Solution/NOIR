namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for EmailChangeOtp entity.
/// </summary>
public class EmailChangeOtpConfiguration : IEntityTypeConfiguration<EmailChangeOtp>
{
    public void Configure(EntityTypeBuilder<EmailChangeOtp> builder)
    {
        builder.ToTable("EmailChangeOtps");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // User ID (required for email change)
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();
        builder.HasIndex(e => e.UserId);

        // Current Email
        builder.Property(e => e.CurrentEmail)
            .HasMaxLength(256)
            .IsRequired();

        // New Email (indexed for checking if already in use)
        builder.Property(e => e.NewEmail)
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(e => e.NewEmail);

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

        // Composite index for active OTPs per user (for bypass prevention)
        builder.HasIndex(e => new { e.UserId, e.IsUsed, e.IsDeleted });

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
    }
}
