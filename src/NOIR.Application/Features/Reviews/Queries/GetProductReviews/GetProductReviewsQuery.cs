namespace NOIR.Application.Features.Reviews.Queries.GetProductReviews;

/// <summary>
/// Query to get reviews for a specific product (public, approved only).
/// </summary>
public sealed record GetProductReviewsQuery(
    Guid ProductId,
    string? Sort = null,
    int Page = 1,
    int PageSize = 20);
