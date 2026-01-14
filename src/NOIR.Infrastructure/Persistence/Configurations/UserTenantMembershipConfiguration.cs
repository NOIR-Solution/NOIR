namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for UserTenantMembership entity.
/// Configures the many-to-many relationship between Users and Tenants.
/// </summary>
public class UserTenantMembershipConfiguration : IEntityTypeConfiguration<UserTenantMembership>
{
    public void Configure(EntityTypeBuilder<UserTenantMembership> builder)
    {
        builder.ToTable("UserTenantMemberships");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // User ID (FK to AspNetUsers)
        // Must match ApplicationUser.Id length (500) from global conventions
        builder.Property(e => e.UserId)
            .HasMaxLength(500)
            .IsRequired();

        // Tenant ID (FK to Tenants)
        // Must match Tenant.Id length (36) from TenantConfiguration
        builder.Property(e => e.TenantId)
            .HasMaxLength(36)
            .IsRequired();

        // Role (stored as int)
        builder.Property(e => e.Role)
            .IsRequired();

        // Unique constraint: one membership per user per tenant
        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_UserTenantMemberships_UserId_TenantId");

        // Index for user lookups: "What tenants does this user belong to?"
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_UserTenantMemberships_UserId");

        // Index for tenant lookups: "What users belong to this tenant?"
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_UserTenantMemberships_TenantId");

        // Filtered unique index: only one default per user (among non-deleted)
        builder.HasIndex(e => new { e.UserId, e.IsDefault })
            .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("IX_UserTenantMemberships_UserId_Default");

        // Relationships
        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.TenantMemberships)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.UserMemberships)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.ModifiedBy).HasMaxLength(450);
        builder.Property(e => e.DeletedBy).HasMaxLength(450);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Soft delete index
        builder.HasIndex(e => e.IsDeleted);
    }
}
