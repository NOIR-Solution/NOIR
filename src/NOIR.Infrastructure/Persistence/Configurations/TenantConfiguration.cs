namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Tenant entity in ApplicationDbContext.
/// Maps to the same "Tenants" table managed by TenantStoreDbContext.
/// This allows ApplicationDbContext to reference tenants without creating a separate table.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Use the same table name as TenantStoreDbContext
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        // Property configurations must match TenantStoreDbContext
        builder.Property(t => t.Id)
            .HasMaxLength(36);

        builder.Property(t => t.Identifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Name)
            .HasMaxLength(200);

        // Audit fields
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.ModifiedBy).HasMaxLength(100);
        builder.Property(t => t.DeletedBy).HasMaxLength(100);

        // Soft delete filter - exclude deleted tenants from queries
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
