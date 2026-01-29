namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ProductAttribute aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class ProductAttributeRepository : Repository<ProductAttribute, Guid>, IRepository<ProductAttribute, Guid>, IScopedService
{
    public ProductAttributeRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
