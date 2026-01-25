namespace NOIR.Infrastructure.Persistence.Configurations;

public class PaymentWebhookLogConfiguration : IEntityTypeConfiguration<PaymentWebhookLog>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookLog> builder)
    {
        builder.ToTable("PaymentWebhookLogs");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Gateway relationship
        builder.Property(e => e.PaymentGatewayId)
            .IsRequired();

        builder.HasIndex(e => e.PaymentGatewayId)
            .HasDatabaseName("IX_PaymentWebhookLogs_GatewayId");

        // Provider
        builder.Property(e => e.Provider)
            .HasMaxLength(50)
            .IsRequired();

        // Event type
        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        // Gateway event ID (for deduplication)
        builder.Property(e => e.GatewayEventId)
            .HasMaxLength(200);

        builder.HasIndex(e => e.GatewayEventId)
            .HasDatabaseName("IX_PaymentWebhookLogs_GatewayEventId");

        // Request body (large text)
        builder.Property(e => e.RequestBody)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Signature valid
        builder.Property(e => e.SignatureValid)
            .HasDefaultValue(false);

        // Processing status
        builder.Property(e => e.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(WebhookProcessingStatus.Received);

        // Processing error
        builder.Property(e => e.ProcessingError)
            .HasMaxLength(2000);

        // Retry count
        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        // Payment transaction relationship
        builder.Property(e => e.PaymentTransactionId);

        builder.HasIndex(e => e.PaymentTransactionId)
            .HasDatabaseName("IX_PaymentWebhookLogs_PaymentTransactionId");

        // IP address
        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        // Status index for retry queries
        builder.HasIndex(e => new { e.ProcessingStatus, e.RetryCount, e.CreatedAt })
            .HasDatabaseName("IX_PaymentWebhookLogs_Status_Retry_Created");

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
