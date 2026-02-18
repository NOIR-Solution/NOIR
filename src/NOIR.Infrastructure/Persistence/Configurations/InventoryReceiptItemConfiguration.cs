using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NOIR.Domain.Entities.Inventory;

namespace NOIR.Infrastructure.Persistence.Configurations;

public class InventoryReceiptItemConfiguration : IEntityTypeConfiguration<InventoryReceiptItem>
{
    public void Configure(EntityTypeBuilder<InventoryReceiptItem> builder)
    {
        builder.ToTable("InventoryReceiptItems");

        builder.HasKey(e => e.Id);

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

        // TenantId index
        builder.HasIndex(e => e.TenantId);

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
