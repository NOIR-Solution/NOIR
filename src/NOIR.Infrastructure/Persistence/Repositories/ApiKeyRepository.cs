namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for ApiKey aggregate root entities.
/// </summary>
public sealed class ApiKeyRepository : Repository<ApiKey, Guid>, IRepository<ApiKey, Guid>, IScopedService
{
    public ApiKeyRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
