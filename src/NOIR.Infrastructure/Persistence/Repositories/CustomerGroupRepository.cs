namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CustomerGroup aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class CustomerGroupRepository : Repository<CustomerGroup, Guid>, IRepository<CustomerGroup, Guid>, IScopedService
{
    public CustomerGroupRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
