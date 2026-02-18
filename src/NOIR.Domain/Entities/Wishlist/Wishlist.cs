namespace NOIR.Domain.Entities.Wishlist;

/// <summary>
/// A user's wishlist for saving products.
/// Supports multiple wishlists per user with sharing capabilities.
/// </summary>
public class Wishlist : TenantAggregateRoot<Guid>
{
    public string UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public bool IsPublic { get; private set; }
    public string? ShareToken { get; private set; }

    // Navigation
    public virtual ICollection<WishlistItem> Items { get; private set; } = new List<WishlistItem>();

    // Computed
    public int ItemCount => Items.Count;

    // Private constructor for EF Core
    private Wishlist() : base() { }

    private Wishlist(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new wishlist.
    /// </summary>
    public static Wishlist Create(string userId, string name, bool isDefault = true, string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Wishlist(Guid.NewGuid(), tenantId)
        {
            UserId = userId,
            Name = name,
            IsDefault = isDefault,
            IsPublic = false
        };
    }

    /// <summary>
    /// Updates the wishlist name.
    /// </summary>
    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Sets the public visibility of the wishlist.
    /// </summary>
    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;

        if (!isPublic)
        {
            ShareToken = null;
        }
    }

    /// <summary>
    /// Generates a unique share token for the wishlist.
    /// </summary>
    public string GenerateShareToken()
    {
        ShareToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        IsPublic = true;
        return ShareToken;
    }

    /// <summary>
    /// Adds a product item to the wishlist.
    /// </summary>
    public WishlistItem AddItem(Guid productId, Guid? productVariantId = null, string? note = null)
    {
        // Check for duplicate
        var existing = Items.FirstOrDefault(i =>
            i.ProductId == productId && i.ProductVariantId == productVariantId);

        if (existing != null)
        {
            return existing;
        }

        var item = WishlistItem.Create(Id, productId, productVariantId, note, TenantId);
        Items.Add(item);
        return item;
    }

    /// <summary>
    /// Removes an item from the wishlist.
    /// </summary>
    public void RemoveItem(Guid wishlistItemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == wishlistItemId);
        if (item != null)
        {
            Items.Remove(item);
        }
    }
}
