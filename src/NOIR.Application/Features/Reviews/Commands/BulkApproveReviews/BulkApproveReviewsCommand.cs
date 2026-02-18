namespace NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;

/// <summary>
/// Command to bulk approve multiple reviews.
/// </summary>
public sealed record BulkApproveReviewsCommand(List<Guid> ReviewIds)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}
