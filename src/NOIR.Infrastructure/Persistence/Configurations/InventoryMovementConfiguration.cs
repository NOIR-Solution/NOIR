using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NOIR.Domain.Entities.Product;

namespace NOIR.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MovementType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Reference)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.UserId)
            .HasMaxLength(450);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        // Foreign keys
        builder.HasOne(e => e.ProductVariant)
            .WithMany()
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries (TenantId first per CLAUDE.md rule #18)
        builder.HasIndex(e => new { e.TenantId, e.ProductVariantId, e.CreatedAt })
            .HasDatabaseName("IX_InventoryMovements_TenantId_VariantId_CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.ProductId, e.CreatedAt })
            .HasDatabaseName("IX_InventoryMovements_TenantId_ProductId_CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.Reference })
            .HasDatabaseName("IX_InventoryMovements_TenantId_Reference");

        builder.HasIndex(e => new { e.TenantId, e.MovementType, e.CreatedAt })
            .HasDatabaseName("IX_InventoryMovements_TenantId_MovementType_CreatedAt");
    }
}
