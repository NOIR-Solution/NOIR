using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NOIR.Domain.Entities.Inventory;

namespace NOIR.Infrastructure.Persistence.Configurations;

public class InventoryReceiptConfiguration : IEntityTypeConfiguration<InventoryReceipt>
{
    public void Configure(EntityTypeBuilder<InventoryReceipt> builder)
    {
        builder.ToTable("InventoryReceipts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        builder.Property(e => e.ConfirmedBy)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        builder.Property(e => e.CancelledBy)
            .HasMaxLength(DatabaseConstants.UserIdMaxLength);

        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // Unique receipt number per tenant
        builder.HasIndex(e => new { e.ReceiptNumber, e.TenantId })
            .IsUnique();

        // Common query indexes (TenantId first per CLAUDE.md rule #18)
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt })
            .HasDatabaseName("IX_InventoryReceipts_TenantId_Status_CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.Type, e.CreatedAt })
            .HasDatabaseName("IX_InventoryReceipts_TenantId_Type_CreatedAt");

        // Standalone TenantId index
        builder.HasIndex(e => e.TenantId);

        // Audit fields
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // Soft delete query filter
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);

        // Navigation
        builder.HasMany(e => e.Items)
            .WithOne(i => i.Receipt)
            .HasForeignKey(i => i.InventoryReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
