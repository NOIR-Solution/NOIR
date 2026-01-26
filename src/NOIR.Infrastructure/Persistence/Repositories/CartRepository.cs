namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Cart aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class CartRepository : Repository<Domain.Entities.Cart.Cart, Guid>, IRepository<Domain.Entities.Cart.Cart, Guid>, IScopedService
{
    public CartRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
