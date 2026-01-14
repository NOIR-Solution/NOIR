namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TenantDomain entity.
/// Configures domain mapping for multi-tenant resolution.
/// </summary>
public class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("TenantDomains");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Tenant ID (FK to Tenants)
        builder.Property(e => e.TenantId)
            .HasMaxLength(36)
            .IsRequired();

        // Domain (unique across all tenants)
        builder.Property(e => e.Domain)
            .HasMaxLength(253) // Max DNS domain length
            .IsRequired();

        // Unique domain constraint (globally unique)
        builder.HasIndex(e => e.Domain)
            .IsUnique()
            .HasDatabaseName("IX_TenantDomains_Domain");

        // Verification token
        builder.Property(e => e.VerificationToken)
            .HasMaxLength(100);

        // Index for tenant domain lookups
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_TenantDomains_TenantId");

        // Index for primary domain lookup
        builder.HasIndex(e => new { e.TenantId, e.IsPrimary })
            .HasFilter("[IsPrimary] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_TenantDomains_TenantId_Primary");

        // Relationship to Tenant
        builder.HasOne<Tenant>()
            .WithMany(t => t.Domains)
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
