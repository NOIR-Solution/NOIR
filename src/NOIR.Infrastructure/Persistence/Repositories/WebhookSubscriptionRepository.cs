namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for WebhookSubscription aggregate root entities.
/// </summary>
public sealed class WebhookSubscriptionRepository : Repository<WebhookSubscription, Guid>, IRepository<WebhookSubscription, Guid>, IScopedService
{
    public WebhookSubscriptionRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
