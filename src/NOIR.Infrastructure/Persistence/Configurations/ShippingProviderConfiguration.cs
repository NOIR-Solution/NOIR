namespace NOIR.Infrastructure.Persistence.Configurations;

public class ShippingProviderConfiguration : IEntityTypeConfiguration<ShippingProvider>
{
    public void Configure(EntityTypeBuilder<ShippingProvider> builder)
    {
        builder.ToTable("ShippingProviders");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Provider code (unique per tenant)
        builder.Property(e => e.ProviderCode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.ProviderCode, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_ShippingProviders_ProviderCode_TenantId");

        // Provider name (official name)
        builder.Property(e => e.ProviderName)
            .HasMaxLength(100)
            .IsRequired();

        // Display name
        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        // Environment
        builder.Property(e => e.Environment)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Encrypted credentials (JSON)
        builder.Property(e => e.EncryptedCredentials)
            .HasColumnType("nvarchar(max)");

        // Webhook URL and secret
        builder.Property(e => e.WebhookUrl)
            .HasMaxLength(500);

        builder.Property(e => e.WebhookSecret)
            .HasMaxLength(256);

        // API configuration
        builder.Property(e => e.ApiBaseUrl)
            .HasMaxLength(500);

        builder.Property(e => e.TrackingUrlTemplate)
            .HasMaxLength(500);

        // Supported services (JSON array string)
        builder.Property(e => e.SupportedServices)
            .HasMaxLength(500)
            .HasDefaultValue("[]");

        // Weight limits
        builder.Property(e => e.MinWeightGrams)
            .HasDefaultValue(0);

        builder.Property(e => e.MaxWeightGrams);

        // COD limits
        builder.Property(e => e.MinCodAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxCodAmount)
            .HasPrecision(18, 2);

        // Feature support
        builder.Property(e => e.SupportsCod)
            .HasDefaultValue(true);

        builder.Property(e => e.SupportsInsurance)
            .HasDefaultValue(false);

        // Health status
        builder.Property(e => e.HealthStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ShippingProviderHealthStatus.Unknown);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Active status
        builder.Property(e => e.IsActive)
            .HasDefaultValue(false);

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Performance index: Active providers per tenant
        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.SortOrder })
            .HasDatabaseName("IX_ShippingProviders_Tenant_Active_Sort");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
