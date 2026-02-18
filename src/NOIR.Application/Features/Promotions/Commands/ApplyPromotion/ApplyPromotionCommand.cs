namespace NOIR.Application.Features.Promotions.Commands.ApplyPromotion;

/// <summary>
/// Command to apply a promotion/voucher code to an order.
/// </summary>
public sealed record ApplyPromotionCommand(
    string Code,
    Guid OrderId,
    decimal OrderTotal) : IAuditableCommand<PromotionUsageDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => OrderId;
    public string? GetTargetDisplayName() => Code;
    public string? GetActionDescription() => $"Applied promotion code '{Code}'";
}
