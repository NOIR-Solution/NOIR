namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Defines a subscription plan that customers can subscribe to.
/// </summary>
public class SubscriptionPlan : TenantAggregateRoot<Guid>
{
    private SubscriptionPlan() : base() { }
    private SubscriptionPlan(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Plan name (e.g., "Basic", "Pro", "Enterprise").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Plan description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Price per billing interval.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Currency code (default: VND).
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// How often the plan is billed.
    /// </summary>
    public BillingInterval Interval { get; private set; }

    /// <summary>
    /// Number of trial days before billing starts.
    /// </summary>
    public int? TrialDays { get; private set; }

    /// <summary>
    /// JSON array of features included in this plan.
    /// </summary>
    public string? FeaturesJson { get; private set; }

    /// <summary>
    /// Whether the plan is available for new subscriptions.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order for UI.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// External gateway plan ID (e.g., Stripe price ID).
    /// </summary>
    public string? ExternalPlanId { get; private set; }

    /// <summary>
    /// Navigation property to subscriptions.
    /// </summary>
    public virtual ICollection<Subscription> Subscriptions { get; private set; } = new List<Subscription>();

    /// <summary>
    /// Creates a new subscription plan.
    /// </summary>
    public static SubscriptionPlan Create(
        string name,
        decimal price,
        BillingInterval interval,
        string currency = "VND",
        int? trialDays = null,
        string? tenantId = null)
    {
        var plan = new SubscriptionPlan(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Price = price,
            Currency = currency,
            Interval = interval,
            TrialDays = trialDays,
            IsActive = true
        };
        plan.AddDomainEvent(new SubscriptionPlanCreatedEvent(plan.Id, name, price, interval));
        return plan;
    }

    public void Update(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }

    public void SetFeatures(string featuresJson)
    {
        FeaturesJson = featuresJson;
    }

    public void SetExternalPlanId(string externalPlanId)
    {
        ExternalPlanId = externalPlanId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
