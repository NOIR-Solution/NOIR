namespace NOIR.Application.Features.Reviews.Commands.VoteReview;

/// <summary>
/// Command to vote on a review's helpfulness.
/// </summary>
public sealed record VoteReviewCommand(Guid ReviewId, bool IsHelpful);
