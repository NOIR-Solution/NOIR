namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Notification aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class NotificationRepository : Repository<Notification, Guid>, IRepository<Notification, Guid>, IScopedService
{
    public NotificationRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
