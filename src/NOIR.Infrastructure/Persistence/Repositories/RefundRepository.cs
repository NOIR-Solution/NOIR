namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Refund entities.
/// </summary>
public sealed class RefundRepository : Repository<Refund, Guid>, IRepository<Refund, Guid>, IScopedService
{
    public RefundRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
