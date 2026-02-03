namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Order aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class OrderRepository : Repository<Domain.Entities.Order.Order, Guid>, IRepository<Domain.Entities.Order.Order, Guid>, IScopedService
{
    public OrderRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
