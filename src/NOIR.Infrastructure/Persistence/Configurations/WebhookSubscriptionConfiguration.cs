namespace NOIR.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : TenantEntityConfiguration<Domain.Entities.Webhook.WebhookSubscription>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Webhook.WebhookSubscription> builder)
    {
        base.Configure(builder);
        builder.ToTable("WebhookSubscriptions");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.Secret).IsRequired().HasMaxLength(128);
        builder.Property(e => e.EventPatterns).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.CustomHeaders).HasMaxLength(4000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => new { e.Url, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_WebhookSubscriptions_Url_TenantId");

        builder.HasIndex(e => new { e.IsActive, e.TenantId })
            .HasDatabaseName("IX_WebhookSubscriptions_IsActive_TenantId");

        builder.HasMany(e => e.DeliveryLogs)
            .WithOne(d => d.Subscription)
            .HasForeignKey(d => d.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
