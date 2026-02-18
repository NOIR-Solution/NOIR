namespace NOIR.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Command to create a new product review.
/// </summary>
public sealed record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string? Title,
    string Content,
    Guid? OrderId,
    List<string>? MediaUrls) : IAuditableCommand<ReviewDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => $"Review for product {ProductId}";
    public string? GetActionDescription() => $"Created review with rating {Rating}";
}
