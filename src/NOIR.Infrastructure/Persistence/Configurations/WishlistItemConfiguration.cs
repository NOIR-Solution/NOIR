namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for WishlistItem entity.
/// </summary>
public class WishlistItemConfiguration : TenantEntityConfiguration<Domain.Entities.Wishlist.WishlistItem>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Wishlist.WishlistItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("WishlistItems");

        // Product reference
        builder.Property(e => e.ProductId)
            .IsRequired();

        builder.Property(e => e.ProductVariantId);

        // Timestamp
        builder.Property(e => e.AddedAt)
            .IsRequired();

        // Note
        builder.Property(e => e.Note)
            .HasMaxLength(500);

        // Priority
        builder.Property(e => e.Priority)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.WishlistItemPriority.None);

        // Relationship with Wishlist
        builder.HasOne(e => e.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(e => e.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Product (no cascade to avoid multiple cascade paths)
        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: no duplicate product+variant per wishlist
        builder.HasIndex(e => new { e.WishlistId, e.ProductId, e.ProductVariantId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_WishlistItems_WishlistId_ProductId_VariantId");

        // Index for product lookup
        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_WishlistItems_ProductId");
    }
}
