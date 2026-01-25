namespace NOIR.Application.Features.Subscriptions.Specifications;

/// <summary>
/// Specification to get a subscription plan by ID.
/// </summary>
public class SubscriptionPlanByIdSpec : Specification<SubscriptionPlan>
{
    public SubscriptionPlanByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .TagWith("SubscriptionPlanById");
    }
}

/// <summary>
/// Specification to get a subscription plan by ID for update.
/// </summary>
public class SubscriptionPlanByIdForUpdateSpec : Specification<SubscriptionPlan>
{
    public SubscriptionPlanByIdForUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsTracking()
             .TagWith("SubscriptionPlanByIdForUpdate");
    }
}

/// <summary>
/// Specification to get active subscription plans.
/// </summary>
public class ActiveSubscriptionPlansSpec : Specification<SubscriptionPlan>
{
    public ActiveSubscriptionPlansSpec()
    {
        Query.Where(p => p.IsActive)
             .OrderBy(p => p.SortOrder)
             .ThenBy(p => p.Name)
             .TagWith("ActiveSubscriptionPlans");
    }
}

/// <summary>
/// Specification to get all subscription plans (admin).
/// </summary>
public class AllSubscriptionPlansSpec : Specification<SubscriptionPlan>
{
    public AllSubscriptionPlansSpec(bool? isActive = null)
    {
        if (isActive.HasValue)
        {
            Query.Where(p => p.IsActive == isActive.Value);
        }

        Query.OrderBy(p => p.SortOrder)
             .ThenBy(p => p.Name)
             .TagWith("AllSubscriptionPlans");
    }
}

/// <summary>
/// Specification to check for duplicate plan name.
/// </summary>
public class SubscriptionPlanByNameSpec : Specification<SubscriptionPlan>
{
    public SubscriptionPlanByNameSpec(string name, Guid? excludeId = null)
    {
        Query.Where(p => p.Name == name);

        if (excludeId.HasValue)
        {
            Query.Where(p => p.Id != excludeId.Value);
        }

        Query.TagWith("SubscriptionPlanByName");
    }
}
