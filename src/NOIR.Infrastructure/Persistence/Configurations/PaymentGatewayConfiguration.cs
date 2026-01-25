namespace NOIR.Infrastructure.Persistence.Configurations;

public class PaymentGatewayConfiguration : IEntityTypeConfiguration<PaymentGateway>
{
    public void Configure(EntityTypeBuilder<PaymentGateway> builder)
    {
        builder.ToTable("PaymentGateways");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Provider (unique per tenant)
        builder.Property(e => e.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.Provider, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_PaymentGateways_Provider_TenantId");

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

        // Supported currencies (JSON array string)
        builder.Property(e => e.SupportedCurrencies)
            .HasMaxLength(500)
            .HasDefaultValue("[]");

        // Health status
        builder.Property(e => e.HealthStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GatewayHealthStatus.Unknown);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Active status
        builder.Property(e => e.IsActive)
            .HasDefaultValue(false);

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Performance index: Active gateways per tenant
        builder.HasIndex(e => new { e.TenantId, e.IsActive, e.SortOrder })
            .HasDatabaseName("IX_PaymentGateways_Tenant_Active_Sort");

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
