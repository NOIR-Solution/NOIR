namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CartItem entity.
/// </summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Product reference
        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.ProductVariantId)
            .IsRequired();

        // Product snapshot
        builder.Property(e => e.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.VariantName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        // Pricing
        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2);

        // Quantity
        builder.Property(e => e.Quantity)
            .IsRequired();

        // Relationship with Cart
        builder.HasOne(e => e.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(e => e.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for finding items by product variant (to check if item exists)
        builder.HasIndex(e => new { e.CartId, e.ProductVariantId })
            .HasDatabaseName("IX_CartItems_CartId_ProductVariantId");

        // Index for product lookup (e.g., when product is deleted)
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_CartItems_ProductId");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);

        // Ignore computed properties
        builder.Ignore(e => e.LineTotal);
    }
}
