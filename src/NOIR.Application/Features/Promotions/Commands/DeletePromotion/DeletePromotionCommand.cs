namespace NOIR.Application.Features.Promotions.Commands.DeletePromotion;

/// <summary>
/// Command to soft delete a promotion.
/// </summary>
public sealed record DeletePromotionCommand(Guid Id) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Promotion";
    public string? GetActionDescription() => "Deleted promotion";
}
