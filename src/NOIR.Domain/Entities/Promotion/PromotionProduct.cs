namespace NOIR.Domain.Entities.Promotion;

/// <summary>
/// Junction entity linking a promotion to a specific product.
/// Used when ApplyLevel is Product.
/// </summary>
public class PromotionProduct : TenantEntity<Guid>
{
    private PromotionProduct() : base() { }

    public PromotionProduct(Guid id, Guid promotionId, Guid productId, string? tenantId)
        : base(id, tenantId)
    {
        PromotionId = promotionId;
        ProductId = productId;
    }

    /// <summary>
    /// Parent promotion ID.
    /// </summary>
    public Guid PromotionId { get; private set; }

    /// <summary>
    /// Target product ID.
    /// </summary>
    public Guid ProductId { get; private set; }

    // Navigation
    public virtual Promotion? Promotion { get; private set; }
}
