namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Permission entity.
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Permission key fields
        builder.Property(e => e.Resource)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(e => e.Action)
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(e => e.Scope)
            .HasMaxLength(50);

        // Unique constraint on resource:action:scope
        builder.HasIndex(e => new { e.Resource, e.Action, e.Scope })
            .IsUnique();

        // Display fields
        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(e => e.Description)
            .HasMaxLength(500);
        builder.Property(e => e.Category)
            .HasMaxLength(100);

        // Ignored computed property
        builder.Ignore(e => e.Name);
    }
}

/// <summary>
/// EF Core configuration for RolePermission join entity.
/// Now extends Entity with audit tracking support.
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        // Primary key using Entity.Id
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Role ID
        builder.Property(e => e.RoleId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength)
            .IsRequired();

        // Unique constraint on (RoleId, PermissionId) - replaces composite key
        builder.HasIndex(e => new { e.RoleId, e.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

        // Individual indexes for lookup
        builder.HasIndex(e => e.RoleId);
        builder.HasIndex(e => e.PermissionId);

        // Relationship to Permission
        builder.HasOne(e => e.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter (from IAuditableEntity)
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Soft delete index
        builder.HasIndex(e => e.IsDeleted);
    }
}
