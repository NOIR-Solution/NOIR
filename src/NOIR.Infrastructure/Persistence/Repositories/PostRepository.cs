namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Post aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class PostRepository : Repository<Post, Guid>, IRepository<Post, Guid>, IScopedService
{
    public PostRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
