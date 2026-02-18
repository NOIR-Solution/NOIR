namespace NOIR.Application.Features.Reviews.Commands.ApproveReview;

/// <summary>
/// Command to approve a product review.
/// </summary>
public sealed record ApproveReviewCommand(Guid ReviewId) : IAuditableCommand<ReviewDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ReviewId;
    public string? GetTargetDisplayName() => "Review";
    public string? GetActionDescription() => "Approved review";
}
