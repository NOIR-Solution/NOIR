namespace NOIR.Application.Features.Wishlists.Queries.GetWishlists;

/// <summary>
/// Query to get all wishlists for the current user.
/// </summary>
public sealed record GetWishlistsQuery
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}
