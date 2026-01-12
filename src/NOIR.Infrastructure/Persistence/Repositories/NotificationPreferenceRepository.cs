namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for NotificationPreference aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class NotificationPreferenceRepository : Repository<NotificationPreference, Guid>, IRepository<NotificationPreference, Guid>, IScopedService
{
    public NotificationPreferenceRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
