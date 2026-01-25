namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for PaymentGateway entities.
/// </summary>
public sealed class PaymentGatewayRepository : Repository<PaymentGateway, Guid>, IRepository<PaymentGateway, Guid>, IScopedService
{
    public PaymentGatewayRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
