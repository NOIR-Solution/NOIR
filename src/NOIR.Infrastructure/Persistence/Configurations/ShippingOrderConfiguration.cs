namespace NOIR.Infrastructure.Persistence.Configurations;

public class ShippingOrderConfiguration : IEntityTypeConfiguration<ShippingOrder>
{
    public void Configure(EntityTypeBuilder<ShippingOrder> builder)
    {
        builder.ToTable("ShippingOrders");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Order reference (FK to Orders table)
        builder.Property(e => e.OrderId).IsRequired();
        builder.HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_ShippingOrders_OrderId");

        // Provider reference
        builder.Property(e => e.ProviderId).IsRequired();
        builder.HasOne(e => e.Provider)
            .WithMany()
            .HasForeignKey(e => e.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Provider code (denormalized for queries)
        builder.Property(e => e.ProviderCode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ProviderOrderId)
            .HasMaxLength(200);

        // Tracking number (globally unique)
        builder.Property(e => e.TrackingNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.TrackingNumber)
            .IsUnique()
            .HasDatabaseName("UX_ShippingOrders_TrackingNumber");

        // Service type
        builder.Property(e => e.ServiceTypeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ServiceTypeName)
            .HasMaxLength(200)
            .IsRequired();

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_ShippingOrders_Status");

        // Fees
        builder.Property(e => e.BaseRate)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CodFee)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(e => e.InsuranceFee)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(e => e.TotalShippingFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CodAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.DeclaredValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.WeightGrams)
            .HasPrecision(10, 2)
            .IsRequired();

        // Address/Contact JSON fields
        builder.Property(e => e.PickupAddressJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.DeliveryAddressJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.SenderJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.RecipientJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.ItemsJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // URLs
        builder.Property(e => e.LabelUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.TrackingUrl)
            .HasMaxLength(1000);

        // Dates
        builder.Property(e => e.EstimatedDeliveryDate);
        builder.Property(e => e.ActualDeliveryDate);
        builder.Property(e => e.PickedUpAt);

        // Notes
        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        // Raw response (for debugging)
        builder.Property(e => e.ProviderRawResponse)
            .HasColumnType("nvarchar(max)");

        // Flags
        builder.Property(e => e.IsFreeship)
            .IsRequired()
            .HasDefaultValue(false);

        // Tenant
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Performance indexes
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt })
            .HasDatabaseName("IX_ShippingOrders_Tenant_Status_Created");

        builder.HasIndex(e => new { e.TenantId, e.OrderId })
            .HasDatabaseName("IX_ShippingOrders_Tenant_OrderId");

        // Tracking events relationship
        builder.HasMany(e => e.TrackingEvents)
            .WithOne(te => te.ShippingOrder)
            .HasForeignKey(te => te.ShippingOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
