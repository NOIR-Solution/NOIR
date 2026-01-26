namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CheckoutSession entity.
/// </summary>
public class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> builder)
    {
        builder.ToTable("CheckoutSessions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Cart reference
        builder.Property(e => e.CartId)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.CartId })
            .HasDatabaseName("IX_CheckoutSessions_TenantId_CartId");

        // User
        builder.Property(e => e.UserId)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_CheckoutSessions_TenantId_UserId");

        // Status
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_CheckoutSessions_TenantId_Status");

        // Expiration
        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.HasIndex(e => new { e.Status, e.ExpiresAt })
            .HasDatabaseName("IX_CheckoutSessions_Status_ExpiresAt");

        // Customer info
        builder.Property(e => e.CustomerEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(e => e.CustomerName)
            .HasMaxLength(200);

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

        // Shipping
        builder.Property(e => e.ShippingMethod)
            .HasMaxLength(100);

        builder.Property(e => e.ShippingCost)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        // Payment
        builder.Property(e => e.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Financial
        builder.Property(e => e.SubTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.DiscountAmount)
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

        // Coupon
        builder.Property(e => e.CouponCode)
            .HasMaxLength(50);

        // Customer notes
        builder.Property(e => e.CustomerNotes)
            .HasMaxLength(1000);

        // Result
        builder.Property(e => e.OrderNumber)
            .HasMaxLength(50);

        builder.HasIndex(e => e.OrderId)
            .HasFilter("[OrderId] IS NOT NULL")
            .HasDatabaseName("IX_CheckoutSessions_OrderId");

        // Navigation to Cart
        builder.HasOne(e => e.Cart)
            .WithMany()
            .HasForeignKey(e => e.CartId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation to Order
        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
    }
}
