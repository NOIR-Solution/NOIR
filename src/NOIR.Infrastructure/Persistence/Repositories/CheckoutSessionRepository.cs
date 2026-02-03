namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CheckoutSession aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class CheckoutSessionRepository : Repository<Domain.Entities.Checkout.CheckoutSession, Guid>, IRepository<Domain.Entities.Checkout.CheckoutSession, Guid>, IScopedService
{
    public CheckoutSessionRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
