namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Product aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class ProductRepository : Repository<Product, Guid>, IRepository<Product, Guid>, IScopedService
{
    public ProductRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
