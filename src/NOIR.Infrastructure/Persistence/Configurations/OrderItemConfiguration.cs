namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OrderItem entity.
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Product reference
        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.ProductVariantId)
            .IsRequired();

        // Product snapshot (denormalized for historical accuracy)
        builder.Property(e => e.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.VariantName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.OptionsSnapshot)
            .HasMaxLength(500);

        // Pricing
        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.DiscountAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(e => e.TaxAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        // Quantity
        builder.Property(e => e.Quantity)
            .IsRequired();

        // Relationship with Order
        builder.HasOne(e => e.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for finding items by product (e.g., for sales analytics)
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        builder.HasIndex(e => e.ProductVariantId)
            .HasDatabaseName("IX_OrderItems_ProductVariantId");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);

        // Ignore computed properties
        builder.Ignore(e => e.LineTotal);
        builder.Ignore(e => e.Subtotal);
    }
}
