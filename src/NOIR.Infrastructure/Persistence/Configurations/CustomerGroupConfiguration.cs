namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CustomerGroup entity.
/// </summary>
public class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("CustomerGroups");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Identity
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Multi-tenant unique constraints (CLAUDE.md Rule 18)
        builder.HasIndex(e => new { e.TenantId, e.Name })
            .IsUnique()
            .HasDatabaseName("IX_CustomerGroups_TenantId_Name");

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_CustomerGroups_TenantId_Slug");

        // Organization
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Cached metrics
        builder.Property(e => e.MemberCount)
            .HasDefaultValue(0);

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

        // Navigation to memberships
        builder.HasMany(e => e.Memberships)
            .WithOne(m => m.CustomerGroup)
            .HasForeignKey(m => m.CustomerGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
