namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Brand aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class BrandRepository : Repository<Brand, Guid>, IRepository<Brand, Guid>, IScopedService
{
    public BrandRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
