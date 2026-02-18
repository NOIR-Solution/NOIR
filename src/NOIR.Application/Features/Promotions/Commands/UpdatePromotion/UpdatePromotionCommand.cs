namespace NOIR.Application.Features.Promotions.Commands.UpdatePromotion;

/// <summary>
/// Command to update an existing promotion.
/// </summary>
public sealed record UpdatePromotionCommand(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    PromotionType PromotionType,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    PromotionApplyLevel ApplyLevel,
    decimal? MaxDiscountAmount = null,
    decimal? MinOrderValue = null,
    int? MinItemQuantity = null,
    int? UsageLimitTotal = null,
    int? UsageLimitPerUser = null,
    List<Guid>? ProductIds = null,
    List<Guid>? CategoryIds = null) : IAuditableCommand<PromotionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated promotion '{Name}'";
}
