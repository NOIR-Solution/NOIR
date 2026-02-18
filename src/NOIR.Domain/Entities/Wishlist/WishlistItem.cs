namespace NOIR.Domain.Entities.Wishlist;

/// <summary>
/// Individual item in a wishlist.
/// References a product and optionally a specific variant.
/// </summary>
public class WishlistItem : TenantEntity<Guid>
{
    public Guid WishlistId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public string? Note { get; private set; }
    public WishlistItemPriority Priority { get; private set; }

    // Navigation
    public virtual Wishlist Wishlist { get; private set; } = null!;
    public virtual Product.Product Product { get; private set; } = null!;

    // Private constructor for EF Core
    private WishlistItem() { }

    /// <summary>
    /// Factory method to create a new wishlist item.
    /// </summary>
    internal static WishlistItem Create(
        Guid wishlistId,
        Guid productId,
        Guid? productVariantId,
        string? note,
        string? tenantId)
    {
        return new WishlistItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WishlistId = wishlistId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            AddedAt = DateTimeOffset.UtcNow,
            Note = note,
            Priority = WishlistItemPriority.None
        };
    }

    /// <summary>
    /// Updates the note on this wishlist item.
    /// </summary>
    public void UpdateNote(string? note)
    {
        Note = note;
    }

    /// <summary>
    /// Updates the priority of this wishlist item.
    /// </summary>
    public void UpdatePriority(WishlistItemPriority priority)
    {
        Priority = priority;
    }
}
