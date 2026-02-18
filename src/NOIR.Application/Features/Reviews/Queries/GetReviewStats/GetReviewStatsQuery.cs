namespace NOIR.Application.Features.Reviews.Queries.GetReviewStats;

/// <summary>
/// Query to get aggregated review statistics for a product.
/// </summary>
public sealed record GetReviewStatsQuery(Guid ProductId);
