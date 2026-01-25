namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Represents a customer's subscription to a plan.
/// </summary>
public class Subscription : TenantAggregateRoot<Guid>
{
    private Subscription() : base() { }
    private Subscription(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Customer who owns this subscription.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// The subscription plan.
    /// </summary>
    public Guid PlanId { get; private set; }

    /// <summary>
    /// Current subscription status.
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>
    /// Start of the current billing period.
    /// </summary>
    public DateTimeOffset CurrentPeriodStart { get; private set; }

    /// <summary>
    /// End of the current billing period.
    /// </summary>
    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    /// <summary>
    /// When the subscription was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// When the trial period ends.
    /// </summary>
    public DateTimeOffset? TrialEnd { get; private set; }

    /// <summary>
    /// Billing interval (copied from plan for historical record).
    /// </summary>
    public BillingInterval Interval { get; private set; }

    /// <summary>
    /// Subscription amount (may differ from plan if discounted).
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// External subscription ID (e.g., Stripe subscription ID).
    /// </summary>
    public string? ExternalSubscriptionId { get; private set; }

    /// <summary>
    /// Whether to cancel at period end instead of immediately.
    /// </summary>
    public bool CancelAtPeriodEnd { get; private set; }

    /// <summary>
    /// JSON metadata for additional subscription data.
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>
    /// Navigation property to plan.
    /// </summary>
    public virtual SubscriptionPlan? Plan { get; private set; }

    /// <summary>
    /// Creates a new subscription.
    /// </summary>
    public static Subscription Create(
        Guid customerId,
        Guid planId,
        BillingInterval interval,
        decimal amount,
        string currency,
        int? trialDays = null,
        string? tenantId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = new Subscription(Guid.NewGuid(), tenantId)
        {
            CustomerId = customerId,
            PlanId = planId,
            Interval = interval,
            Amount = amount,
            Currency = currency,
            Status = trialDays.HasValue ? SubscriptionStatus.Trialing : SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays((int)interval),
            TrialEnd = trialDays.HasValue ? now.AddDays(trialDays.Value) : null
        };
        subscription.AddDomainEvent(new SubscriptionCreatedEvent(
            subscription.Id, customerId, planId, amount, subscription.Status));
        return subscription;
    }

    public void SetExternalSubscriptionId(string externalSubscriptionId)
    {
        ExternalSubscriptionId = externalSubscriptionId;
    }

    public void Activate()
    {
        var oldStatus = Status;
        Status = SubscriptionStatus.Active;
        AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
    }

    public void MarkAsPastDue()
    {
        var oldStatus = Status;
        Status = SubscriptionStatus.PastDue;
        AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
    }

    public void Cancel(bool atPeriodEnd = true)
    {
        CancelAtPeriodEnd = atPeriodEnd;
        CancelledAt = DateTimeOffset.UtcNow;

        if (!atPeriodEnd)
        {
            var oldStatus = Status;
            Status = SubscriptionStatus.Cancelled;
            AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
        }

        AddDomainEvent(new SubscriptionCancelledEvent(Id, CustomerId, atPeriodEnd));
    }

    public void Pause()
    {
        var oldStatus = Status;
        Status = SubscriptionStatus.Paused;
        AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
    }

    public void Resume()
    {
        if (Status != SubscriptionStatus.Paused)
            throw new InvalidOperationException("Only paused subscriptions can be resumed");

        var oldStatus = Status;
        Status = SubscriptionStatus.Active;
        AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
    }

    public void Expire()
    {
        var oldStatus = Status;
        Status = SubscriptionStatus.Expired;
        AddDomainEvent(new SubscriptionStatusChangedEvent(Id, oldStatus, Status));
    }

    public void RenewPeriod()
    {
        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd = CurrentPeriodStart.AddDays((int)Interval);
        AddDomainEvent(new SubscriptionRenewedEvent(Id, CustomerId, Amount, CurrentPeriodEnd));
    }

    public void UpdateAmount(decimal newAmount)
    {
        Amount = newAmount;
    }

    public void SetMetadata(string metadataJson)
    {
        MetadataJson = metadataJson;
    }
}
