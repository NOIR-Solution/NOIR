namespace NOIR.Application.Features.Promotions.Commands.DeactivatePromotion;

/// <summary>
/// Command to deactivate a promotion.
/// </summary>
public sealed record DeactivatePromotionCommand(Guid Id) : IAuditableCommand<PromotionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Promotion";
    public string? GetActionDescription() => "Deactivated promotion";
}
