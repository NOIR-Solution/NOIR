namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EmployeeTag aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class EmployeeTagRepository : Repository<EmployeeTag, Guid>, IRepository<EmployeeTag, Guid>, IScopedService
{
    public EmployeeTagRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
