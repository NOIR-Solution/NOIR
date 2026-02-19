namespace NOIR.Infrastructure.Persistence.Configurations;

public class InventoryReceiptItemConfiguration : TenantEntityConfiguration<InventoryReceiptItem>
{
    public override void Configure(EntityTypeBuilder<InventoryReceiptItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("InventoryReceiptItems");

        builder.Property(e => e.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.VariantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Sku)
            .HasMaxLength(100);

        builder.Property(e => e.UnitCost)
            .HasPrecision(18, 4);

        // Ignore computed property
        builder.Ignore(e => e.LineTotal);

        // Index for receipt items lookup
        builder.HasIndex(e => e.InventoryReceiptId)
            .HasDatabaseName("IX_InventoryReceiptItems_ReceiptId");
    }
}
