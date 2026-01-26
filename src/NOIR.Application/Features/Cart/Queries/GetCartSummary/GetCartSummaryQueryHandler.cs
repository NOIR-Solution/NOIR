namespace NOIR.Application.Features.Cart.Queries.GetCartSummary;

/// <summary>
/// Handler for getting cart summary for mini-cart.
/// </summary>
public sealed class GetCartSummaryQueryHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;

    public GetCartSummaryQueryHandler(IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartSummaryDto>> Handle(GetCartSummaryQuery query, CancellationToken ct)
    {
        Domain.Entities.Cart.Cart? cart = null;

        // Try to get cart by user ID first, then session ID
        if (!string.IsNullOrEmpty(query.UserId))
        {
            var userCartSpec = new ActiveCartByUserIdSpec(query.UserId, forUpdate: false);
            cart = await _cartRepository.FirstOrDefaultAsync(userCartSpec, ct);
        }

        if (cart is null && !string.IsNullOrEmpty(query.SessionId))
        {
            var sessionCartSpec = new ActiveCartBySessionIdSpec(query.SessionId, forUpdate: false);
            cart = await _cartRepository.FirstOrDefaultAsync(sessionCartSpec, ct);
        }

        // Return empty summary if no cart found
        if (cart is null)
        {
            return Result.Success(new CartSummaryDto
            {
                Id = Guid.Empty,
                ItemCount = 0,
                Subtotal = 0,
                Currency = "VND",
                RecentItems = new List<CartItemSummaryDto>()
            });
        }

        return Result.Success(CartMapper.ToSummaryDto(cart, recentItemCount: 5));
    }
}
