namespace NOIR.Application.Features.Wishlists.Queries.GetWishlistById;

/// <summary>
/// Query to get a wishlist by ID with full item details.
/// </summary>
public sealed record GetWishlistByIdQuery(Guid Id)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}
