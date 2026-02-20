namespace NOIR.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Order ID (nullable until Phase 8 - Order entity)
        builder.Property(e => e.OrderId);

        builder.HasIndex(e => new { e.OrderId, e.TenantId })
            .HasDatabaseName("IX_PaymentTransactions_OrderId_TenantId");

        // Gateway relationship
        builder.HasOne(e => e.Gateway)
            .WithMany()
            .HasForeignKey(e => e.PaymentGatewayId)
            .OnDelete(DeleteBehavior.Restrict);

        // Provider
        builder.Property(e => e.Provider)
            .HasMaxLength(50)
            .IsRequired();

        // Transaction number (unique per tenant for lookups)
        builder.Property(e => e.TransactionNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.TransactionNumber, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_PaymentTransactions_TransactionNumber_TenantId");

        // Gateway transaction ID
        builder.Property(e => e.GatewayTransactionId)
            .HasMaxLength(200);

        builder.HasIndex(e => e.GatewayTransactionId)
            .HasDatabaseName("IX_PaymentTransactions_GatewayTransactionId");

        // Amount
        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Currency
        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Payment method
        builder.Property(e => e.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(PaymentStatus.Pending);

        // Return URL
        builder.Property(e => e.ReturnUrl)
            .HasMaxLength(2000);

        // Failure reason
        builder.Property(e => e.FailureReason)
            .HasMaxLength(1000);

        // Idempotency key (unique per tenant)
        builder.Property(e => e.IdempotencyKey)
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.IdempotencyKey, e.TenantId })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_PaymentTransactions_IdempotencyKey_TenantId");

        // COD fields
        builder.Property(e => e.CodCollectorName)
            .HasMaxLength(200);

        // Metadata (JSON)
        builder.Property(e => e.MetadataJson)
            .HasColumnType("nvarchar(max)");

        // Status indexes for common queries
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt })
            .HasDatabaseName("IX_PaymentTransactions_Tenant_Status_Created");

        builder.HasIndex(e => new { e.PaymentMethod, e.Status })
            .HasDatabaseName("IX_PaymentTransactions_Method_Status");

        builder.HasIndex(e => new { e.Status, e.ExpiresAt })
            .HasDatabaseName("IX_PaymentTransactions_Status_Expires");

        // Concurrency token for optimistic concurrency control
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Tenant ID
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
