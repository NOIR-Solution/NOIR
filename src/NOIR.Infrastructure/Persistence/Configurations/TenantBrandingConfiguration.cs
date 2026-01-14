namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for TenantBranding entity.
/// Configures white-label branding settings for tenants.
/// </summary>
public class TenantBrandingConfiguration : IEntityTypeConfiguration<TenantBranding>
{
    public void Configure(EntityTypeBuilder<TenantBranding> builder)
    {
        builder.ToTable("TenantBranding");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Tenant ID (FK to Tenants, one-to-one)
        builder.Property(e => e.TenantId)
            .HasMaxLength(36)
            .IsRequired();

        // Unique constraint: one branding per tenant
        builder.HasIndex(e => e.TenantId)
            .IsUnique()
            .HasDatabaseName("IX_TenantBranding_TenantId");

        // Logo URLs
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.LogoDarkUrl).HasMaxLength(500);
        builder.Property(e => e.FaviconUrl).HasMaxLength(500);

        // Color settings (hex codes like #3B82F6)
        builder.Property(e => e.PrimaryColor).HasMaxLength(20);
        builder.Property(e => e.SecondaryColor).HasMaxLength(20);
        builder.Property(e => e.AccentColor).HasMaxLength(20);
        builder.Property(e => e.BackgroundColor).HasMaxLength(20);
        builder.Property(e => e.TextColor).HasMaxLength(20);

        // Relationship to Tenant (one-to-one)
        builder.HasOne<Tenant>()
            .WithOne(t => t.Branding)
            .HasForeignKey<TenantBranding>(e => e.TenantId)
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
