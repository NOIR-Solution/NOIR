namespace NOIR.Application.Features.Promotions.Commands.CreatePromotion;

/// <summary>
/// Command to create a new promotion.
/// </summary>
public sealed record CreatePromotionCommand(
    string Name,
    string Code,
    string? Description,
    PromotionType PromotionType,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    PromotionApplyLevel ApplyLevel = PromotionApplyLevel.Cart,
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

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created promotion '{Name}' (Code: {Code})";
}
