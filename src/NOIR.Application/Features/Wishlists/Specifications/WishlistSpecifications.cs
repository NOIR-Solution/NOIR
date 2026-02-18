namespace NOIR.Application.Features.Wishlists.Specifications;

/// <summary>
/// Specification to get a wishlist by ID with items and product info.
/// </summary>
public sealed class WishlistByIdSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public WishlistByIdSpec(Guid wishlistId, bool forUpdate = false)
    {
        Query.Where(w => w.Id == wishlistId)
            .Include(w => w.Items)
            .TagWith("WishlistById");

        if (forUpdate)
        {
            Query.AsTracking();
        }
    }
}

/// <summary>
/// Specification to get a wishlist by ID with items including product navigation.
/// Used for detail queries that need product information.
/// </summary>
public sealed class WishlistDetailByIdSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public WishlistDetailByIdSpec(Guid wishlistId)
    {
        Query.Where(w => w.Id == wishlistId)
            .Include("Items.Product.Images")
            .Include("Items.Product.Variants")
            .TagWith("WishlistDetailById");
    }
}

/// <summary>
/// Specification to get all wishlists for a user.
/// </summary>
public sealed class WishlistsByUserSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public WishlistsByUserSpec(string userId)
    {
        Query.Where(w => w.UserId == userId)
            .Include(w => w.Items)
            .OrderByDescending(w => w.IsDefault)
            .ThenByDescending(w => w.CreatedAt)
            .TagWith("WishlistsByUser");
    }
}

/// <summary>
/// Specification to get a default wishlist for a user (for update).
/// </summary>
public sealed class DefaultWishlistByUserSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public DefaultWishlistByUserSpec(string userId, bool forUpdate = false)
    {
        Query.Where(w => w.UserId == userId && w.IsDefault)
            .Include(w => w.Items)
            .TagWith("DefaultWishlistByUser");

        if (forUpdate)
        {
            Query.AsTracking();
        }
    }
}

/// <summary>
/// Specification to get a shared wishlist by share token.
/// </summary>
public sealed class WishlistByShareTokenSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public WishlistByShareTokenSpec(string shareToken)
    {
        Query.Where(w => w.ShareToken == shareToken && w.IsPublic)
            .Include("Items.Product.Images")
            .Include("Items.Product.Variants")
            .TagWith("WishlistByShareToken");
    }
}

/// <summary>
/// Specification to get a wishlist item by ID (for update).
/// </summary>
public sealed class WishlistItemByIdSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public WishlistItemByIdSpec(Guid itemId)
    {
        Query.Where(w => w.Items.Any(i => i.Id == itemId))
            .Include(w => w.Items)
            .AsTracking()
            .TagWith("WishlistItemById");
    }
}

/// <summary>
/// Specification to get all wishlists with items and product info (for analytics).
/// </summary>
public sealed class AllWishlistsWithItemsSpec : Specification<Domain.Entities.Wishlist.Wishlist>
{
    public AllWishlistsWithItemsSpec()
    {
        Query.Include("Items.Product.Images")
            .TagWith("AllWishlistsWithItems");
    }
}
