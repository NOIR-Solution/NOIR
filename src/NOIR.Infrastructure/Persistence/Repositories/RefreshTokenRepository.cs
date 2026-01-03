namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RefreshToken aggregate root.
/// Registered as scoped service via IScopedService marker interface.
/// </summary>
public sealed class RefreshTokenRepository : Repository<RefreshToken, Guid>, IScopedService
{
    public RefreshTokenRepository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
        : base(dbContext, currentUser, dateTime)
    {
    }
}
