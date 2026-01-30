namespace NOIR.Infrastructure.Persistence.Configurations;

public class ShippingTrackingEventConfiguration : IEntityTypeConfiguration<ShippingTrackingEvent>
{
    public void Configure(EntityTypeBuilder<ShippingTrackingEvent> builder)
    {
        builder.ToTable("ShippingTrackingEvents");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Shipping order reference
        builder.Property(e => e.ShippingOrderId).IsRequired();

        // Event type from provider
        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        // Mapped status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsRequired();

        // Location
        builder.Property(e => e.Location)
            .HasMaxLength(500);

        // Dates
        builder.Property(e => e.EventDate).IsRequired();
        builder.Property(e => e.ReceivedAt).IsRequired();

        // Raw payload for debugging
        builder.Property(e => e.RawPayload)
            .HasColumnType("nvarchar(max)");

        // Tenant
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);

        // Performance index: events by shipping order and date
        builder.HasIndex(e => new { e.ShippingOrderId, e.EventDate })
            .IsDescending(false, true)
            .HasDatabaseName("IX_ShippingTrackingEvents_Order_Date");

    }
}
