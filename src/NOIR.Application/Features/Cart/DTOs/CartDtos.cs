namespace NOIR.Application.Features.Cart.DTOs;

/// <summary>
/// Full cart details for cart page.
/// </summary>
public record CartDto
{
    public Guid Id { get; init; }
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public CartStatus Status { get; init; }
    public string Currency { get; init; } = "VND";
    public DateTimeOffset LastActivityAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public List<CartItemDto> Items { get; init; } = new();

    // Computed
    public int ItemCount => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public bool IsEmpty => !Items.Any();
    public bool IsGuest => string.IsNullOrEmpty(UserId);
}

/// <summary>
/// Individual cart item details.
/// </summary>
public record CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// Lightweight cart summary for mini-cart and header display.
/// </summary>
public record CartSummaryDto
{
    public Guid Id { get; init; }
    public int ItemCount { get; init; }
    public decimal Subtotal { get; init; }
    public string Currency { get; init; } = "VND";
    public List<CartItemSummaryDto> RecentItems { get; init; } = new();
}

/// <summary>
/// Abbreviated item info for mini-cart.
/// </summary>
public record CartItemSummaryDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Request to add an item to cart.
/// </summary>
public record AddToCartRequest
{
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public int Quantity { get; init; } = 1;
}

/// <summary>
/// Request to update cart item quantity.
/// ItemId comes from the route parameter.
/// </summary>
public record UpdateCartItemRequest
{
    public int Quantity { get; init; }
}

/// <summary>
/// Result of cart merge operation.
/// </summary>
public record CartMergeResultDto
{
    public Guid TargetCartId { get; init; }
    public int MergedItemCount { get; init; }
    public int TotalItemCount { get; init; }
    public decimal NewSubtotal { get; init; }
}
