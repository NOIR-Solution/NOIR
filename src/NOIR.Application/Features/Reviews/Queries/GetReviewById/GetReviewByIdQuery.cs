namespace NOIR.Application.Features.Reviews.Queries.GetReviewById;

/// <summary>
/// Query to get a single review by ID with full details.
/// </summary>
public sealed record GetReviewByIdQuery(Guid Id);
