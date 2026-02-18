namespace NOIR.Application.Features.Wishlists.DTOs;

/// <summary>
/// Summary view of a wishlist (for list pages).
/// </summary>
public record WishlistDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public bool IsDefault { get; init; }
    public bool IsPublic { get; init; }
    public string? ShareUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Detail view of a wishlist including items with product info.
/// </summary>
public record WishlistDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public bool IsDefault { get; init; }
    public bool IsPublic { get; init; }
    public string? ShareUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<WishlistItemDto> Items { get; init; } = new();
}

/// <summary>
/// Individual wishlist item with product information.
/// </summary>
public record WishlistItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductImage { get; init; }
    public decimal Price { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string? VariantName { get; init; }
    public DateTimeOffset AddedAt { get; init; }
    public string? Note { get; init; }
    public WishlistItemPriority Priority { get; init; }
    public bool IsInStock { get; init; }
}

/// <summary>
/// Request to add an item to a wishlist.
/// </summary>
public record AddToWishlistRequest
{
    public Guid? WishlistId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// Request to update wishlist item priority.
/// </summary>
public record UpdateWishlistItemPriorityRequest
{
    public WishlistItemPriority Priority { get; init; }
}

/// <summary>
/// Wishlist analytics for admin view.
/// </summary>
public record WishlistAnalyticsDto
{
    public int TotalWishlists { get; init; }
    public int TotalWishlistItems { get; init; }
    public List<TopWishlistedProductDto> TopProducts { get; init; } = new();
}

/// <summary>
/// Top wishlisted product info.
/// </summary>
public record TopWishlistedProductDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductImage { get; init; }
    public int WishlistCount { get; init; }
}
