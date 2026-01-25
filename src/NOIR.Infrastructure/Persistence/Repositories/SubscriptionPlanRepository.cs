namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for SubscriptionPlan entities.
/// </summary>
public sealed class SubscriptionPlanRepository : Repository<SubscriptionPlan, Guid>, IRepository<SubscriptionPlan, Guid>, IScopedService
{
    public SubscriptionPlanRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
