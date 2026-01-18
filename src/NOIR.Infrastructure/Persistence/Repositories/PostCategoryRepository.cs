namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PostCategory aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class PostCategoryRepository : Repository<PostCategory, Guid>, IRepository<PostCategory, Guid>, IScopedService
{
    public PostCategoryRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
