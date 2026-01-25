namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for PaymentTransaction entities.
/// </summary>
public sealed class PaymentTransactionRepository : Repository<PaymentTransaction, Guid>, IRepository<PaymentTransaction, Guid>, IScopedService
{
    public PaymentTransactionRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
