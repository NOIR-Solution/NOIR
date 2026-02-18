namespace NOIR.Application.Features.Wishlists.Common;

/// <summary>
/// Maps Wishlist entities to DTOs.
/// </summary>
public static class WishlistMapper
{
    public static WishlistDto ToDto(Domain.Entities.Wishlist.Wishlist wishlist)
    {
        return new WishlistDto
        {
            Id = wishlist.Id,
            Name = wishlist.Name,
            ItemCount = wishlist.Items.Count,
            IsDefault = wishlist.IsDefault,
            IsPublic = wishlist.IsPublic,
            ShareUrl = wishlist.ShareToken != null ? $"/wishlists/shared/{wishlist.ShareToken}" : null,
            CreatedAt = wishlist.CreatedAt
        };
    }

    public static WishlistDetailDto ToDetailDto(Domain.Entities.Wishlist.Wishlist wishlist)
    {
        return new WishlistDetailDto
        {
            Id = wishlist.Id,
            Name = wishlist.Name,
            ItemCount = wishlist.Items.Count,
            IsDefault = wishlist.IsDefault,
            IsPublic = wishlist.IsPublic,
            ShareUrl = wishlist.ShareToken != null ? $"/wishlists/shared/{wishlist.ShareToken}" : null,
            CreatedAt = wishlist.CreatedAt,
            Items = wishlist.Items.Select(ToItemDto).ToList()
        };
    }

    public static WishlistItemDto ToItemDto(Domain.Entities.Wishlist.WishlistItem item)
    {
        var product = item.Product;
        var variant = product?.Variants?.FirstOrDefault(v => v.Id == item.ProductVariantId);

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = product?.Name ?? string.Empty,
            ProductImage = product?.PrimaryImage?.Url,
            Price = variant?.Price ?? product?.BasePrice ?? 0,
            ProductVariantId = item.ProductVariantId,
            VariantName = variant?.Name,
            AddedAt = item.AddedAt,
            Note = item.Note,
            Priority = item.Priority,
            IsInStock = variant != null ? variant.StockQuantity > 0 : product?.InStock ?? false
        };
    }
}
