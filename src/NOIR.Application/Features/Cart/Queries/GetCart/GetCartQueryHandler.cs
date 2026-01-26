namespace NOIR.Application.Features.Cart.Queries.GetCart;

/// <summary>
/// Handler for getting the current cart.
/// </summary>
public sealed class GetCartQueryHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;

    public GetCartQueryHandler(IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery query, CancellationToken ct)
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

        // Return empty cart DTO if no cart found
        if (cart is null)
        {
            return Result.Success(new CartDto
            {
                Id = Guid.Empty,
                UserId = query.UserId,
                SessionId = query.SessionId,
                Status = CartStatus.Active,
                Currency = "VND",
                LastActivityAt = DateTimeOffset.UtcNow,
                Items = new List<CartItemDto>()
            });
        }

        return Result.Success(CartMapper.ToDto(cart));
    }
}
