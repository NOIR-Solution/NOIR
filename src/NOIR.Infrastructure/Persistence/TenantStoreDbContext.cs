using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;

namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Separate DbContext for Finbuckle tenant storage.
/// Required because EFCoreStore needs a DbContext that inherits from EFCoreStoreDbContext{T},
/// but ApplicationDbContext already inherits from IdentityDbContext.
/// </summary>
public class TenantStoreDbContext : EFCoreStoreDbContext<Tenant>
{
    public TenantStoreDbContext(DbContextOptions<TenantStoreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply the same configuration as in ApplicationDbContext for Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(t => t.Id);

            // Unique identifier constraint
            entity.HasIndex(t => t.Identifier)
                .IsUnique()
                .HasDatabaseName("IX_Tenants_Identifier");

            // Property configurations
            entity.Property(t => t.Id)
                .HasMaxLength(36);

            entity.Property(t => t.Identifier)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.Name)
                .HasMaxLength(200);

            entity.Property(t => t.LogoUrl)
                .HasMaxLength(500);

            entity.Property(t => t.PrimaryColor)
                .HasMaxLength(50);

            entity.Property(t => t.AccentColor)
                .HasMaxLength(50);

            entity.Property(t => t.Theme)
                .HasMaxLength(50);

            // Audit fields
            entity.Property(t => t.CreatedBy).HasMaxLength(100);
            entity.Property(t => t.ModifiedBy).HasMaxLength(100);
            entity.Property(t => t.DeletedBy).HasMaxLength(100);

            // Soft delete filter - exclude deleted tenants from queries
            entity.HasQueryFilter(t => !t.IsDeleted);
        });
    }
}
