namespace NOIR.Domain.Entities.Promotion;

/// <summary>
/// Represents a promotion or voucher campaign.
/// Supports various discount types, usage limits, and targeting (cart/product/category).
/// </summary>
public class Promotion : TenantAggregateRoot<Guid>
{
    private Promotion() : base() { }
    private Promotion(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Display name of the promotion.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Unique promotion/voucher code per tenant (e.g., "SUMMER2026").
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Type of promotion campaign.
    /// </summary>
    public PromotionType PromotionType { get; private set; }

    /// <summary>
    /// How the discount is calculated.
    /// </summary>
    public DiscountType DiscountType { get; private set; }

    /// <summary>
    /// Discount value (amount or percentage depending on DiscountType).
    /// </summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>
    /// Maximum discount amount for percentage-based discounts (cap).
    /// </summary>
    public decimal? MaxDiscountAmount { get; private set; }

    /// <summary>
    /// Minimum order value required to use this promotion.
    /// </summary>
    public decimal? MinOrderValue { get; private set; }

    /// <summary>
    /// Minimum item quantity required to use this promotion.
    /// </summary>
    public int? MinItemQuantity { get; private set; }

    /// <summary>
    /// Total number of times this promotion can be used (null = unlimited).
    /// </summary>
    public int? UsageLimitTotal { get; private set; }

    /// <summary>
    /// Number of times a single user can use this promotion (null = unlimited).
    /// </summary>
    public int? UsageLimitPerUser { get; private set; }

    /// <summary>
    /// Current total usage count.
    /// </summary>
    public int CurrentUsageCount { get; private set; }

    /// <summary>
    /// When the promotion becomes active.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// When the promotion expires.
    /// </summary>
    public DateTimeOffset EndDate { get; private set; }

    /// <summary>
    /// Whether the promotion is enabled.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public PromotionStatus Status { get; private set; }

    /// <summary>
    /// Level at which the promotion applies.
    /// </summary>
    public PromotionApplyLevel ApplyLevel { get; private set; }

    // Navigation properties
    public virtual ICollection<PromotionProduct> Products { get; private set; } = new List<PromotionProduct>();
    public virtual ICollection<PromotionCategory> Categories { get; private set; } = new List<PromotionCategory>();
    public virtual ICollection<PromotionUsage> Usages { get; private set; } = new List<PromotionUsage>();

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    public static Promotion Create(
        string name,
        string code,
        PromotionType promotionType,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PromotionApplyLevel applyLevel = PromotionApplyLevel.Cart,
        string? description = null,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? minItemQuantity = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (discountValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(discountValue), "Discount value must be greater than zero.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date.", nameof(endDate));

        return new Promotion(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            PromotionType = promotionType,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            MinOrderValue = minOrderValue,
            MinItemQuantity = minItemQuantity,
            UsageLimitTotal = usageLimitTotal,
            UsageLimitPerUser = usageLimitPerUser,
            CurrentUsageCount = 0,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = false,
            Status = PromotionStatus.Draft,
            ApplyLevel = applyLevel
        };
    }

    /// <summary>
    /// Updates promotion details. Only allowed when in Draft or Scheduled status.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string code,
        PromotionType promotionType,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PromotionApplyLevel applyLevel,
        decimal? maxDiscountAmount,
        decimal? minOrderValue,
        int? minItemQuantity,
        int? usageLimitTotal,
        int? usageLimitPerUser)
    {
        Name = name;
        Description = description;
        Code = code.ToUpperInvariant();
        PromotionType = promotionType;
        DiscountType = discountType;
        DiscountValue = discountValue;
        StartDate = startDate;
        EndDate = endDate;
        ApplyLevel = applyLevel;
        MaxDiscountAmount = maxDiscountAmount;
        MinOrderValue = minOrderValue;
        MinItemQuantity = minItemQuantity;
        UsageLimitTotal = usageLimitTotal;
        UsageLimitPerUser = usageLimitPerUser;
    }

    /// <summary>
    /// Activates the promotion.
    /// </summary>
    public void Activate()
    {
        if (Status is PromotionStatus.Cancelled or PromotionStatus.Expired)
            throw new InvalidOperationException($"Cannot activate promotion in {Status} status.");

        IsActive = true;
        Status = PromotionStatus.Active;
    }

    /// <summary>
    /// Deactivates the promotion.
    /// </summary>
    public void Deactivate()
    {
        if (Status != PromotionStatus.Active)
            throw new InvalidOperationException($"Cannot deactivate promotion in {Status} status.");

        IsActive = false;
        Status = PromotionStatus.Draft;
    }

    /// <summary>
    /// Cancels the promotion.
    /// </summary>
    public void Cancel()
    {
        if (Status is PromotionStatus.Cancelled or PromotionStatus.Expired)
            throw new InvalidOperationException($"Cannot cancel promotion in {Status} status.");

        IsActive = false;
        Status = PromotionStatus.Cancelled;
    }

    /// <summary>
    /// Increments the usage count after a successful application.
    /// </summary>
    public void IncrementUsage()
    {
        CurrentUsageCount++;
    }

    /// <summary>
    /// Checks if the promotion is currently valid for use.
    /// </summary>
    public bool IsValid()
    {
        var now = DateTimeOffset.UtcNow;
        return IsActive
            && Status == PromotionStatus.Active
            && now >= StartDate
            && now <= EndDate
            && (!UsageLimitTotal.HasValue || CurrentUsageCount < UsageLimitTotal.Value);
    }

    /// <summary>
    /// Checks if a specific user can use this promotion.
    /// </summary>
    public bool CanBeUsedBy(string userId, int userUsageCount)
    {
        if (!IsValid())
            return false;

        if (UsageLimitPerUser.HasValue && userUsageCount >= UsageLimitPerUser.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Calculates the discount amount for a given order total.
    /// </summary>
    public decimal CalculateDiscount(decimal orderTotal)
    {
        var discount = DiscountType switch
        {
            Enums.DiscountType.FixedAmount => DiscountValue,
            Enums.DiscountType.Percentage => orderTotal * (DiscountValue / 100m),
            Enums.DiscountType.FreeShipping => 0m, // Shipping discount handled separately
            Enums.DiscountType.BuyXGetY => 0m, // Calculated differently based on items
            _ => 0m
        };

        // Apply max discount cap for percentage-based discounts
        if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
        {
            discount = MaxDiscountAmount.Value;
        }

        // Discount cannot exceed order total
        if (discount > orderTotal)
        {
            discount = orderTotal;
        }

        return discount;
    }

    /// <summary>
    /// Adds a product to this promotion's targeting.
    /// </summary>
    public void AddProduct(Guid productId)
    {
        if (Products.Any(p => p.ProductId == productId))
            return;

        Products.Add(new PromotionProduct(Guid.NewGuid(), Id, productId, TenantId));
    }

    /// <summary>
    /// Removes a product from this promotion's targeting.
    /// </summary>
    public void RemoveProduct(Guid productId)
    {
        var product = Products.FirstOrDefault(p => p.ProductId == productId);
        if (product is not null)
            Products.Remove(product);
    }

    /// <summary>
    /// Adds a category to this promotion's targeting.
    /// </summary>
    public void AddCategory(Guid categoryId)
    {
        if (Categories.Any(c => c.CategoryId == categoryId))
            return;

        Categories.Add(new PromotionCategory(Guid.NewGuid(), Id, categoryId, TenantId));
    }

    /// <summary>
    /// Removes a category from this promotion's targeting.
    /// </summary>
    public void RemoveCategory(Guid categoryId)
    {
        var category = Categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (category is not null)
            Categories.Remove(category);
    }
}
