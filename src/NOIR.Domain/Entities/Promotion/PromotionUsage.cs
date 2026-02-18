namespace NOIR.Domain.Entities.Promotion;

/// <summary>
/// Tracks usage of a promotion by a user on a specific order.
/// </summary>
public class PromotionUsage : TenantEntity<Guid>
{
    private PromotionUsage() : base() { }

    public PromotionUsage(Guid id, Guid promotionId, string userId, Guid orderId, decimal discountAmount, string? tenantId)
        : base(id, tenantId)
    {
        PromotionId = promotionId;
        UserId = userId;
        OrderId = orderId;
        DiscountAmount = discountAmount;
        UsedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// The promotion that was used.
    /// </summary>
    public Guid PromotionId { get; private set; }

    /// <summary>
    /// The user who used the promotion.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// The order the promotion was applied to.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The actual discount amount applied.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// When the promotion was used.
    /// </summary>
    public DateTimeOffset UsedAt { get; private set; }

    // Navigation
    public virtual Promotion? Promotion { get; private set; }
}
