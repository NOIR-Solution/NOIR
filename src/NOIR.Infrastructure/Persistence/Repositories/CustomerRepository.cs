namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Customer aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class CustomerRepository : Repository<Domain.Entities.Customer.Customer, Guid>, IRepository<Domain.Entities.Customer.Customer, Guid>, IScopedService
{
    public CustomerRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
