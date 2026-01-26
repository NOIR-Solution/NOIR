namespace NOIR.Application.Features.Cart.Queries.GetCart;

/// <summary>
/// Query to get the current user's or guest's cart.
/// </summary>
public sealed record GetCartQuery
{
    /// <summary>
    /// User ID if authenticated.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Session ID for guest users.
    /// </summary>
    public string? SessionId { get; init; }
}
