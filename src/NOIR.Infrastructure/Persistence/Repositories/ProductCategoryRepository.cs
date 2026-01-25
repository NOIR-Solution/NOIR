namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ProductCategory aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class ProductCategoryRepository : Repository<ProductCategory, Guid>, IRepository<ProductCategory, Guid>, IScopedService
{
    public ProductCategoryRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
