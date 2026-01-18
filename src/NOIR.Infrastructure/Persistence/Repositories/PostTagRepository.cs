namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PostTag aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class PostTagRepository : Repository<PostTag, Guid>, IRepository<PostTag, Guid>, IScopedService
{
    public PostTagRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
