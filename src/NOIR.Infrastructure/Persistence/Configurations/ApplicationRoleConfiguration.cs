namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ApplicationRole entity.
/// </summary>
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        // Table is already "AspNetRoles" from Identity base
        // We just add our custom fields

        // Self-referencing relationship for role hierarchy
        builder.HasOne(r => r.ParentRole)
            .WithMany(r => r.ChildRoles)
            .HasForeignKey(r => r.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Tenant scoping
        builder.Property(r => r.TenantId);
        builder.HasIndex(r => r.TenantId);

        // Description and display fields
        builder.Property(r => r.Description)
            .HasMaxLength(500);
        builder.Property(r => r.IconName)
            .HasMaxLength(50);
        builder.Property(r => r.Color)
            .HasMaxLength(50);

        // Index for hierarchy lookup
        builder.HasIndex(r => r.ParentRoleId);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
        builder.HasIndex(r => r.IsDeleted);

        // Unique index on Name + TenantId (roles can have same name across tenants)
        builder.HasIndex(r => new { r.NormalizedName, r.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_AspNetRoles_NormalizedName_TenantId");
    }
}
