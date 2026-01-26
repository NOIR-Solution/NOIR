namespace NOIR.Application.Features.Cart.Queries.GetCartSummary;

/// <summary>
/// Query to get cart summary for mini-cart display.
/// </summary>
public sealed record GetCartSummaryQuery
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
