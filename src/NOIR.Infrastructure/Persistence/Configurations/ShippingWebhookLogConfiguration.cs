namespace NOIR.Infrastructure.Persistence.Configurations;

public class ShippingWebhookLogConfiguration : IEntityTypeConfiguration<ShippingWebhookLog>
{
    public void Configure(EntityTypeBuilder<ShippingWebhookLog> builder)
    {
        builder.ToTable("ShippingWebhookLogs");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Provider code
        builder.Property(e => e.ProviderCode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Tracking number (nullable, extracted from payload)
        builder.Property(e => e.TrackingNumber)
            .HasMaxLength(100);

        // HTTP info
        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("POST");

        builder.Property(e => e.Endpoint)
            .HasMaxLength(500)
            .IsRequired();

        // Headers and body
        builder.Property(e => e.HeadersJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Body)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.Signature)
            .HasMaxLength(500);

        // Processing status
        builder.Property(e => e.ProcessedSuccessfully)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ProcessingAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        // Timestamps
        builder.Property(e => e.ReceivedAt).IsRequired();
        builder.Property(e => e.ProcessedAt);

        // Indexes for querying
        builder.HasIndex(e => e.ReceivedAt)
            .IsDescending()
            .HasDatabaseName("IX_ShippingWebhookLogs_ReceivedAt");

        builder.HasIndex(e => new { e.ProviderCode, e.TrackingNumber })
            .HasDatabaseName("IX_ShippingWebhookLogs_Provider_Tracking");

        builder.HasIndex(e => new { e.ProcessedSuccessfully, e.ProcessingAttempts })
            .HasDatabaseName("IX_ShippingWebhookLogs_ProcessingStatus");
    }
}
