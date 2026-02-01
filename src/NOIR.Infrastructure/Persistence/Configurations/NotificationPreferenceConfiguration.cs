namespace NOIR.Infrastructure.Persistence.Configurations;

using NOIR.Domain.Enums;

/// <summary>
/// EF Core configuration for NotificationPreference entity.
/// </summary>
public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        // User ID (references AspNetUsers)
        builder.Property(p => p.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // Category (stored as int)
        builder.Property(p => p.Category)
            .HasConversion<int>()
            .IsRequired();

        // Preferences
        builder.Property(p => p.InAppEnabled)
            .HasDefaultValue(true);

        builder.Property(p => p.EmailFrequency)
            .HasConversion<int>()
            .HasDefaultValue(EmailFrequency.Daily);

        // Unique constraint - one preference per user per category per tenant (CLAUDE.md Rule 18)
        builder.HasIndex(p => new { p.TenantId, p.UserId, p.Category })
            .IsUnique()
            .HasDatabaseName("IX_NotificationPreferences_TenantId_UserId_Category");

        // Index for preference lookups
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_NotificationPreferences_UserId");

        // Tenant ID
        builder.Property(p => p.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(p => p.TenantId);

        // Audit fields
        builder.Property(p => p.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(p => p.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(p => p.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}
