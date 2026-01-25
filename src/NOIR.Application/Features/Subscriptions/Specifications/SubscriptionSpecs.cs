namespace NOIR.Application.Features.Subscriptions.Specifications;

/// <summary>
/// Specification to get a subscription by ID.
/// </summary>
public class SubscriptionByIdSpec : Specification<Subscription>
{
    public SubscriptionByIdSpec(Guid id)
    {
        Query.Where(s => s.Id == id)
             .Include(s => s.Plan!)
             .TagWith("SubscriptionById");
    }
}

/// <summary>
/// Specification to get a subscription by ID for update.
/// </summary>
public class SubscriptionByIdForUpdateSpec : Specification<Subscription>
{
    public SubscriptionByIdForUpdateSpec(Guid id)
    {
        Query.Where(s => s.Id == id)
             .AsTracking()
             .TagWith("SubscriptionByIdForUpdate");
    }
}

/// <summary>
/// Specification to get subscriptions by customer.
/// </summary>
public class SubscriptionsByCustomerSpec : Specification<Subscription>
{
    public SubscriptionsByCustomerSpec(Guid customerId)
    {
        Query.Where(s => s.CustomerId == customerId)
             .Include(s => s.Plan!)
             .OrderByDescending(s => s.CreatedAt)
             .TagWith("SubscriptionsByCustomer");
    }
}

/// <summary>
/// Specification to get active subscriptions by customer.
/// </summary>
public class ActiveSubscriptionByCustomerSpec : Specification<Subscription>
{
    public ActiveSubscriptionByCustomerSpec(Guid customerId)
    {
        Query.Where(s => s.CustomerId == customerId &&
                        (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
             .Include(s => s.Plan!)
             .TagWith("ActiveSubscriptionByCustomer");
    }
}

/// <summary>
/// Specification to get subscriptions expiring soon (for renewal processing).
/// </summary>
public class SubscriptionsExpiringSoonSpec : Specification<Subscription>
{
    public SubscriptionsExpiringSoonSpec(int daysBeforeExpiry = 3)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddDays(daysBeforeExpiry);

        Query.Where(s => s.Status == SubscriptionStatus.Active &&
                        s.CurrentPeriodEnd <= expiryThreshold &&
                        !s.CancelAtPeriodEnd)
             .TagWith("SubscriptionsExpiringSoon");
    }
}

/// <summary>
/// Specification to get subscriptions needing cancellation.
/// </summary>
public class SubscriptionsToTerminateSpec : Specification<Subscription>
{
    public SubscriptionsToTerminateSpec()
    {
        Query.Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue) &&
                        s.CancelAtPeriodEnd &&
                        s.CurrentPeriodEnd <= DateTimeOffset.UtcNow)
             .TagWith("SubscriptionsToTerminate");
    }
}
