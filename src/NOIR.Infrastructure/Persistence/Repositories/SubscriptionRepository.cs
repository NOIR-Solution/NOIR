namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Subscription entities.
/// </summary>
public sealed class SubscriptionRepository : Repository<Subscription, Guid>, IRepository<Subscription, Guid>, IScopedService
{
    public SubscriptionRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
