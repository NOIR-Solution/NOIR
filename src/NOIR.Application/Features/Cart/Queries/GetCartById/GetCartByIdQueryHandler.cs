using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.Features.Cart.Queries.GetCartById;

/// <summary>
/// Handler for getting a cart by its ID.
/// </summary>
public sealed class GetCartByIdQueryHandler
{
    private readonly IRepository<Domain.Entities.Cart.Cart, Guid> _cartRepository;

    public GetCartByIdQueryHandler(IRepository<Domain.Entities.Cart.Cart, Guid> cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartDto>> Handle(GetCartByIdQuery query, CancellationToken ct)
    {
        var spec = new CartByIdSpec(query.CartId, forUpdate: false);
        var cart = await _cartRepository.FirstOrDefaultAsync(spec, ct);

        if (cart is null)
        {
            return Result.Failure<CartDto>(
                Error.NotFound($"Cart with ID '{query.CartId}' not found.", "NOIR-CART-001"));
        }

        return Result.Success(CartMapper.ToDto(cart));
    }
}
