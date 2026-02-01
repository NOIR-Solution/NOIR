namespace NOIR.Application.Features.Cart.Queries.GetCartById;

/// <summary>
/// Query to get a cart by its ID.
/// </summary>
public sealed record GetCartByIdQuery(Guid CartId);
