namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Order entity.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Order number (unique per tenant)
        builder.Property(e => e.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        // TenantId leading for Finbuckle multi-tenant filtering
        builder.HasIndex(e => new { e.TenantId, e.OrderNumber })
            .IsUnique()
            .HasDatabaseName("IX_Orders_TenantId_OrderNumber");

        // Customer
        builder.Property(e => e.CustomerId);
        builder.HasIndex(e => new { e.TenantId, e.CustomerId })
            .HasDatabaseName("IX_Orders_TenantId_CustomerId");

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Orders_TenantId_Status");

        // Financial
        builder.Property(e => e.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.ShippingAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.GrandTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        // Shipping Address (Owned Type)
        builder.OwnsOne(e => e.ShippingAddress, address =>
        {
            address.Property(a => a.FullName).HasColumnName("ShippingFullName").HasMaxLength(100);
            address.Property(a => a.Phone).HasColumnName("ShippingPhone").HasMaxLength(20);
            address.Property(a => a.AddressLine1).HasColumnName("ShippingAddressLine1").HasMaxLength(200);
            address.Property(a => a.AddressLine2).HasColumnName("ShippingAddressLine2").HasMaxLength(200);
            address.Property(a => a.Ward).HasColumnName("ShippingWard").HasMaxLength(100);
            address.Property(a => a.District).HasColumnName("ShippingDistrict").HasMaxLength(100);
            address.Property(a => a.Province).HasColumnName("ShippingProvince").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
            address.Property(a => a.IsDefault).HasColumnName("ShippingIsDefault");
        });

        // Billing Address (Owned Type)
        builder.OwnsOne(e => e.BillingAddress, address =>
        {
            address.Property(a => a.FullName).HasColumnName("BillingFullName").HasMaxLength(100);
            address.Property(a => a.Phone).HasColumnName("BillingPhone").HasMaxLength(20);
            address.Property(a => a.AddressLine1).HasColumnName("BillingAddressLine1").HasMaxLength(200);
            address.Property(a => a.AddressLine2).HasColumnName("BillingAddressLine2").HasMaxLength(200);
            address.Property(a => a.Ward).HasColumnName("BillingWard").HasMaxLength(100);
            address.Property(a => a.District).HasColumnName("BillingDistrict").HasMaxLength(100);
            address.Property(a => a.Province).HasColumnName("BillingProvince").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("BillingPostalCode").HasMaxLength(20);
            address.Property(a => a.IsDefault).HasColumnName("BillingIsDefault");
        });

        // Shipping details
        builder.Property(e => e.ShippingMethod)
            .HasMaxLength(100);

        builder.Property(e => e.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(e => e.ShippingCarrier)
            .HasMaxLength(100);

        // Customer info
        builder.Property(e => e.CustomerEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200);

        builder.HasIndex(e => new { e.TenantId, e.CustomerEmail })
            .HasDatabaseName("IX_Orders_TenantId_CustomerEmail");

        // Notes
        builder.Property(e => e.CustomerNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.InternalNotes)
            .HasColumnType("nvarchar(max)");

        // Cancellation
        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // Coupon
        builder.Property(e => e.CouponCode)
            .HasMaxLength(50);

        // Checkout session reference
        builder.Property(e => e.CheckoutSessionId);
        builder.HasIndex(e => e.CheckoutSessionId)
            .HasDatabaseName("IX_Orders_CheckoutSessionId");

        // Date indexes for reporting
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_Orders_TenantId_CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt })
            .HasDatabaseName("IX_Orders_TenantId_Status_CreatedAt");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
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
