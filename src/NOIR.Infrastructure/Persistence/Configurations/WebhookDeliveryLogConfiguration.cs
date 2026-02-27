namespace NOIR.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryLogConfiguration : TenantEntityConfiguration<Domain.Entities.Webhook.WebhookDeliveryLog>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Webhook.WebhookDeliveryLog> builder)
    {
        base.Configure(builder);
        builder.ToTable("WebhookDeliveryLogs");

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.RequestUrl).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.RequestBody).IsRequired();
        builder.Property(e => e.RequestHeaders).HasMaxLength(4000);
        builder.Property(e => e.ResponseBody);
        builder.Property(e => e.ResponseHeaders).HasMaxLength(4000);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => new { e.WebhookSubscriptionId, e.CreatedAt })
            .HasDatabaseName("IX_WebhookDeliveryLogs_SubscriptionId_CreatedAt");

        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasDatabaseName("IX_WebhookDeliveryLogs_Status_NextRetryAt");
    }
}
