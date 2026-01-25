namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for PaymentWebhookLog entities.
/// </summary>
public sealed class PaymentWebhookLogRepository : Repository<PaymentWebhookLog, Guid>, IRepository<PaymentWebhookLog, Guid>, IScopedService
{
    public PaymentWebhookLogRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
