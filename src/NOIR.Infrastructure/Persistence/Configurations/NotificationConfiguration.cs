namespace NOIR.Infrastructure.Persistence.Configurations;

using NOIR.Domain.Enums;
using NOIR.Domain.ValueObjects;
using System.Text.Json;

/// <summary>
/// EF Core configuration for Notification entity.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        // Primary key
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        // User ID (references AspNetUsers)
        builder.Property(n => n.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // Type and Category (stored as int)
        builder.Property(n => n.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(n => n.Category)
            .HasConversion<int>()
            .IsRequired();

        // Title and Message
        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(2000)
            .IsRequired();

        // Optional fields
        builder.Property(n => n.IconClass)
            .HasMaxLength(100);

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(2000);

        builder.Property(n => n.Metadata)
            .HasColumnType("nvarchar(max)");

        // Read status
        builder.Property(n => n.IsRead)
            .HasDefaultValue(false);

        // Email tracking
        builder.Property(n => n.EmailSent)
            .HasDefaultValue(false);

        builder.Property(n => n.IncludedInDigest)
            .HasDefaultValue(false);

        // Actions collection - stored as JSON
        builder.Property(n => n.Actions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<NotificationAction>>(v, (JsonSerializerOptions?)null) ?? new List<NotificationAction>())
            .HasColumnName("Actions")
            .HasColumnType("nvarchar(max)");

        // Indexes for efficient queries
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");

        builder.HasIndex(n => new { n.UserId, n.IsDeleted })
            .HasDatabaseName("IX_Notifications_UserId_IsDeleted");

        builder.HasIndex(n => new { n.UserId, n.Category })
            .HasDatabaseName("IX_Notifications_UserId_Category");

        // Filtered index for unread notifications (TenantId leading for Finbuckle)
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.CreatedAt })
            .HasFilter("[IsRead] = 0")
            .HasDatabaseName("IX_Notifications_Unread");

        // Filtered index for pending digest (TenantId leading for Finbuckle)
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.CreatedAt })
            .HasFilter("[IncludedInDigest] = 0")
            .HasDatabaseName("IX_Notifications_PendingDigest");

        // Filtered index for unsent email notifications (TenantId leading for Finbuckle)
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.CreatedAt })
            .HasFilter("[EmailSent] = 0")
            .HasDatabaseName("IX_Notifications_UnsentEmail");

        // Tenant ID
        builder.Property(n => n.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(n => n.TenantId);

        // Audit fields
        builder.Property(n => n.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(n => n.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(n => n.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(n => n.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", n => !n.IsDeleted);
    }
}
