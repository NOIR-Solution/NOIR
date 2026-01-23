namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for LegalPage aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class LegalPageRepository : Repository<LegalPage, Guid>, IScopedService
{
    public LegalPageRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
