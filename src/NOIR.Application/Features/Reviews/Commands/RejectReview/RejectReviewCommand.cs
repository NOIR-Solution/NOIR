namespace NOIR.Application.Features.Reviews.Commands.RejectReview;

/// <summary>
/// Command to reject a product review.
/// </summary>
public sealed record RejectReviewCommand(Guid ReviewId, string? Reason) : IAuditableCommand<ReviewDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ReviewId;
    public string? GetTargetDisplayName() => "Review";
    public string? GetActionDescription() => "Rejected review";
}
