namespace NOIR.Application.Features.Reviews.Queries.GetReviews;

/// <summary>
/// Query to get reviews with pagination and filtering (admin moderation queue).
/// </summary>
public sealed record GetReviewsQuery(
    int Page = 1,
    int PageSize = 20,
    ReviewStatus? Status = null,
    Guid? ProductId = null,
    int? Rating = null,
    string? Search = null);
