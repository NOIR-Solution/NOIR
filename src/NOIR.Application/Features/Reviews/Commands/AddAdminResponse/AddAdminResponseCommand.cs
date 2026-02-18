namespace NOIR.Application.Features.Reviews.Commands.AddAdminResponse;

/// <summary>
/// Command to add an admin response to a review.
/// </summary>
public sealed record AddAdminResponseCommand(Guid ReviewId, string Response) : IAuditableCommand<ReviewDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => ReviewId;
    public string? GetTargetDisplayName() => "Review";
    public string? GetActionDescription() => "Added admin response to review";
}
