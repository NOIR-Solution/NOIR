namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for PaymentOperationLog entities.
/// </summary>
public sealed class PaymentOperationLogRepository : Repository<PaymentOperationLog, Guid>, IRepository<PaymentOperationLog, Guid>, IScopedService
{
    public PaymentOperationLogRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
