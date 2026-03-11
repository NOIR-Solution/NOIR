namespace NOIR.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : TenantEntityConfiguration<Domain.Entities.ApiKey>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.ApiKey> builder)
    {
        base.Configure(builder);
        builder.ToTable("ApiKeys");

        builder.Property(e => e.KeyIdentifier).IsRequired().HasMaxLength(64);
        builder.Property(e => e.SecretHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.SecretSuffix).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.Permissions).IsRequired().HasMaxLength(8000);
        builder.Property(e => e.LastUsedIp).HasMaxLength(45);
        builder.Property(e => e.RevokedReason).HasMaxLength(500);

        // KeyIdentifier is globally unique (prefix makes it identifiable)
        builder.HasIndex(e => e.KeyIdentifier)
            .IsUnique()
            .HasDatabaseName("IX_ApiKeys_KeyIdentifier");

        // Fast lookup by user within tenant
        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .HasDatabaseName("IX_ApiKeys_UserId_TenantId");

        // Active keys filter (for admin listing)
        builder.HasIndex(e => new { e.IsRevoked, e.TenantId })
            .HasDatabaseName("IX_ApiKeys_IsRevoked_TenantId");
    }
}
