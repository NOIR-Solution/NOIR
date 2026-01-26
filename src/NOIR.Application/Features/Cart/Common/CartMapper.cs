namespace NOIR.Application.Features.Cart.Common;

/// <summary>
/// Maps Cart entities to DTOs.
/// </summary>
public static class CartMapper
{
    public static CartDto ToDto(Domain.Entities.Cart.Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            SessionId = cart.SessionId,
            Status = cart.Status,
            Currency = cart.Currency,
            LastActivityAt = cart.LastActivityAt,
            ExpiresAt = cart.ExpiresAt,
            Items = cart.Items.Select(ToDto).ToList()
        };
    }

    public static CartItemDto ToDto(CartItem item)
    {
        return new CartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            ProductName = item.ProductName,
            VariantName = item.VariantName,
            ImageUrl = item.ImageUrl,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity
        };
    }

    public static CartSummaryDto ToSummaryDto(Domain.Entities.Cart.Cart cart, int recentItemCount = 5)
    {
        return new CartSummaryDto
        {
            Id = cart.Id,
            ItemCount = cart.ItemCount,
            Subtotal = cart.Subtotal,
            Currency = cart.Currency,
            RecentItems = cart.Items
                .OrderByDescending(i => i.CreatedAt)
                .Take(recentItemCount)
                .Select(ToItemSummaryDto)
                .ToList()
        };
    }

    public static CartItemSummaryDto ToItemSummaryDto(CartItem item)
    {
        return new CartItemSummaryDto
        {
            Id = item.Id,
            ProductName = item.ProductName,
            VariantName = item.VariantName,
            ImageUrl = item.ImageUrl,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity
        };
    }
}
