namespace NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;

/// <summary>
/// Command to bulk reject multiple reviews.
/// </summary>
public sealed record BulkRejectReviewsCommand(List<Guid> ReviewIds, string? Reason)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}
