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
            .HasMaxLength(450)
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

        // Backing field for Actions (owned entity workaround)
        builder.Ignore(n => n.Actions);
        builder.Property<string>("ActionsJson")
            .HasColumnName("Actions")
            .HasColumnType("nvarchar(max)");

        // Indexes for efficient queries
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");

        builder.HasIndex(n => new { n.UserId, n.IsDeleted })
            .HasDatabaseName("IX_Notifications_UserId_IsDeleted");

        builder.HasIndex(n => new { n.UserId, n.Category })
            .HasDatabaseName("IX_Notifications_UserId_Category");

        // Index for digest job
        builder.HasIndex(n => new { n.UserId, n.IncludedInDigest, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_PendingDigest");

        // Tenant ID
        builder.Property(n => n.TenantId).HasMaxLength(64);
        builder.HasIndex(n => n.TenantId);

        // Audit fields
        builder.Property(n => n.CreatedBy).HasMaxLength(450);
        builder.Property(n => n.ModifiedBy).HasMaxLength(450);
        builder.Property(n => n.DeletedBy).HasMaxLength(450);
        builder.Property(n => n.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", n => !n.IsDeleted);
    }
}
