namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for MediaFile aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class MediaFileRepository : Repository<MediaFile, Guid>, IRepository<MediaFile, Guid>, IScopedService
{
    public MediaFileRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
