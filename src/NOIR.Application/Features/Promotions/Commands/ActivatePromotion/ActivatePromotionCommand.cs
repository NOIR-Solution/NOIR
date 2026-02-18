namespace NOIR.Application.Features.Promotions.Commands.ActivatePromotion;

/// <summary>
/// Command to activate a promotion.
/// </summary>
public sealed record ActivatePromotionCommand(Guid Id) : IAuditableCommand<PromotionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Promotion";
    public string? GetActionDescription() => "Activated promotion";
}
