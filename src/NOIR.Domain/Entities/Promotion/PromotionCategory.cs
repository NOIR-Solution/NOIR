namespace NOIR.Domain.Entities.Promotion;

/// <summary>
/// Junction entity linking a promotion to a product category.
/// Used when ApplyLevel is Category.
/// </summary>
public class PromotionCategory : TenantEntity<Guid>
{
    private PromotionCategory() : base() { }

    public PromotionCategory(Guid id, Guid promotionId, Guid categoryId, string? tenantId)
        : base(id, tenantId)
    {
        PromotionId = promotionId;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Parent promotion ID.
    /// </summary>
    public Guid PromotionId { get; private set; }

    /// <summary>
    /// Target category ID.
    /// </summary>
    public Guid CategoryId { get; private set; }

    // Navigation
    public virtual Promotion? Promotion { get; private set; }
}
